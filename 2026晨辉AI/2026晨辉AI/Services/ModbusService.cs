using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NModbus;
using NModbus.Device;
using System.Linq;

namespace _2026晨辉AI.Services
{
    public class ModbusService
    {
        public string ModbusIp { get; set; }
        public int ModbusPort { get; set; }
        public int ModbusSlaveId { get; set; }
        private readonly ILogger<ModbusService> _logger;
        // 系统级长连接管理
        private static Dictionary<string, TcpClient> _longConnections = new Dictionary<string, TcpClient>();
        private static readonly object _connectionLock = new object();

        public ModbusService(IConfiguration configuration, ILogger<ModbusService> logger)
        {
            ModbusIp = configuration["Modbus:Ip"] ?? "127.0.0.1";
            ModbusPort = int.TryParse(configuration["Modbus:Port"], out var port) ? port : 502;
            ModbusSlaveId = int.TryParse(configuration["Modbus:SlaveId"], out var slaveId) ? slaveId : 1;
            _logger = logger;
        }

        /// <summary>
        /// 获取或创建长连接
        /// </summary>
        private TcpClient GetOrCreateLongConnection(string connectionKey, string ip, int port)
        {
            lock (_connectionLock)
            {
                // 检查现有连接是否有效
                if (_longConnections.TryGetValue(connectionKey, out var client) && client.Connected)
                {
                    _logger.LogInformation($"使用现有长连接到 {ip}:{port}");
                    return client;
                }
                
                // 创建新的长连接
                _logger.LogInformation($"创建新的长连接到 {ip}:{port}");
                client = new TcpClient();
                
                try
                {
                    // 同步连接，超时时间2秒
                    client.Connect(ip, port);
                    _longConnections[connectionKey] = client;
                    _logger.LogInformation($"长连接创建成功到 {ip}:{port}");
                    return client;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"创建长连接失败: {ex.Message}");
                    client.Dispose();
                    return null;
                }
            }
        }

        /// <summary>
        /// 获取Modbus主站（使用长连接）
        /// </summary>
        private dynamic GetModbusMaster(string ip, int port)
        {
            string connectionKey = $"{ip}:{port}";
            TcpClient client = GetOrCreateLongConnection(connectionKey, ip, port);
            
            if (client == null)
            {
                return null;
            }
            
            var factory = new ModbusFactory();
            var master = factory.CreateMaster(client);
            // 设置操作超时时间为3秒
            master.Transport.ReadTimeout = 3000;
            master.Transport.WriteTimeout = 3000;
            
            return master;
        }

        /// <summary>
        /// 移除无效连接
        /// </summary>
        private void RemoveInvalidConnection(string connectionKey)
        {
            lock (_connectionLock)
            {
                if (_longConnections.TryGetValue(connectionKey, out var client))
                {
                    try { client.Dispose(); } catch { }
                    _longConnections.Remove(connectionKey);
                    _logger.LogInformation($"移除无效长连接: {connectionKey}");
                }
            }
        }

        /// <summary>
        /// 与Modbus设备建立TCP连接并发送报文（写入保持寄存器）
        /// </summary>
        public async Task<ModbusResponse> SendMessageAsync(ushort startAddress, ushort[] values)
        {
            try
            {
                _logger.LogInformation($"发送Modbus报文到 {ModbusIp}:{ModbusPort}");
                _logger.LogInformation($"从站地址: {ModbusSlaveId}, 起始地址: {startAddress}, 值: {string.Join(", ", values)}");
                
                // 使用长连接获取Modbus主站
                var master = GetModbusMaster(ModbusIp, ModbusPort);
                if (master == null)
                {
                    _logger.LogError($"无法创建Modbus主站: 无法连接到设备 {ModbusIp}:{ModbusPort}");
                    return new ModbusResponse(false, "无法创建Modbus主站: 无法连接到设备");
                }

                // 写入多个保持寄存器
                await master.WriteMultipleRegistersAsync((byte)ModbusSlaveId, startAddress, values);

                _logger.LogInformation($"Modbus报文发送成功到 {ModbusIp}:{ModbusPort}");
                return new ModbusResponse(true, "发送成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"发送失败: {ex.Message}");
                // 发生异常时，移除无效连接
                string connectionKey = $"{ModbusIp}:{ModbusPort}";
                RemoveInvalidConnection(connectionKey);
                return new ModbusResponse(false, $"发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 与Modbus设备建立TCP连接并发送报文（写入单个保持寄存器）
        /// </summary>
        public async Task<ModbusResponse> WriteSingleRegisterAsync(ushort address, ushort value)
        {
            try
            {
                _logger.LogInformation($"发送Modbus写入单个寄存器报文到 {ModbusIp}:{ModbusPort}");
                _logger.LogInformation($"从站地址: {ModbusSlaveId}, 地址: {address}, 值: {value}");
                
                // 使用长连接获取Modbus主站
                var master = GetModbusMaster(ModbusIp, ModbusPort);
                if (master == null)
                {
                    _logger.LogError($"无法创建Modbus主站: 无法连接到设备 {ModbusIp}:{ModbusPort}");
                    return new ModbusResponse(false, "无法创建Modbus主站: 无法连接到设备");
                }

                // 写入单个保持寄存器
                await master.WriteSingleRegisterAsync((byte)ModbusSlaveId, address, value);

                _logger.LogInformation($"Modbus写入单个寄存器报文发送成功到 {ModbusIp}:{ModbusPort}");
                return new ModbusResponse(true, "发送成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"发送失败: {ex.Message}");
                // 发生异常时，移除无效连接
                string connectionKey = $"{ModbusIp}:{ModbusPort}";
                RemoveInvalidConnection(connectionKey);
                return new ModbusResponse(false, $"发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 与指定IP和端口的Modbus设备建立TCP连接并发送报文（写入单个保持寄存器）
        /// </summary>
        public async Task<ModbusResponse> WriteSingleRegisterAsync(string ip, int port, byte slaveId, ushort address, ushort value)
        {
            try
            {
                _logger.LogInformation($"发送Modbus写入单个寄存器报文到 {ip}:{port}");
                _logger.LogInformation($"从站地址: {slaveId}, 地址: {address}, 值: {value}");
                
                // 使用长连接获取Modbus主站
                var master = GetModbusMaster(ip, port);
                if (master == null)
                {
                    _logger.LogError($"无法创建Modbus主站: 无法连接到设备 {ip}:{port}");
                    return new ModbusResponse(false, "无法创建Modbus主站: 无法连接到设备");
                }

                // 写入单个保持寄存器
                await master.WriteSingleRegisterAsync(slaveId, address, value);

                _logger.LogInformation($"Modbus写入单个寄存器报文发送成功到 {ip}:{port}");
                return new ModbusResponse(true, "发送成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"发送失败: {ex.Message}");
                // 发生异常时，移除无效连接
                string connectionKey = $"{ip}:{port}";
                RemoveInvalidConnection(connectionKey);
                return new ModbusResponse(false, $"发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 与Modbus设备建立TCP连接并发送报文（写入线圈）
        /// </summary>
        public async Task<ModbusResponse> SendCoilMessageAsync(ushort startAddress, bool[] values)
        {
            try
            {
                _logger.LogInformation($"发送Modbus写入线圈报文到 {ModbusIp}:{ModbusPort}");
                _logger.LogInformation($"从站地址: {ModbusSlaveId}, 起始地址: {startAddress}, 值: {string.Join(", ", values)}");
                
                // 使用长连接获取Modbus主站
                var master = GetModbusMaster(ModbusIp, ModbusPort);
                if (master == null)
                {
                    _logger.LogError($"无法创建Modbus主站: 无法连接到设备 {ModbusIp}:{ModbusPort}");
                    return new ModbusResponse(false, "无法创建Modbus主站: 无法连接到设备");
                }

                // 写入多个线圈
                await master.WriteMultipleCoilsAsync((byte)ModbusSlaveId, startAddress, values);

                _logger.LogInformation($"Modbus写入线圈报文发送成功到 {ModbusIp}:{ModbusPort}");
                return new ModbusResponse(true, "发送成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"发送失败: {ex.Message}");
                // 发生异常时，移除无效连接
                string connectionKey = $"{ModbusIp}:{ModbusPort}";
                RemoveInvalidConnection(connectionKey);
                return new ModbusResponse(false, $"发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取保持寄存器
        /// </summary>
        public async Task<ModbusReadResponse> ReadRegistersAsync(ushort startAddress, ushort count)
        {
            try
            {
                _logger.LogInformation($"从 {ModbusIp}:{ModbusPort} 读取保持寄存器");
                _logger.LogInformation($"从站地址: {ModbusSlaveId}, 起始地址: {startAddress}, 数量: {count}");
                
                // 使用长连接获取Modbus主站
                var master = GetModbusMaster(ModbusIp, ModbusPort);
                if (master == null)
                {
                    _logger.LogError($"无法创建Modbus主站: 无法连接到设备 {ModbusIp}:{ModbusPort}");
                    return new ModbusReadResponse(false, "无法创建Modbus主站: 无法连接到设备", Array.Empty<ushort>());
                }

                // 读取保持寄存器
                var values = await master.ReadHoldingRegistersAsync((byte)ModbusSlaveId, startAddress, count);
                
                _logger.LogInformation($"读取成功: {string.Join(", ", values)}");
                return new ModbusReadResponse(true, "读取成功", values);
            }
            catch (Exception ex)
            {
                _logger.LogError($"读取失败: {ex.Message}");
                // 发生异常时，移除无效连接
                string connectionKey = $"{ModbusIp}:{ModbusPort}";
                RemoveInvalidConnection(connectionKey);
                return new ModbusReadResponse(false, $"读取失败: {ex.Message}", Array.Empty<ushort>());
            }
        }

        /// <summary>
        /// 读取输入寄存器（04功能码）
        /// </summary>
        public async Task<ModbusReadResponse> ReadInputRegistersAsync(ushort startAddress, ushort count)
        {
            try
            {
                _logger.LogInformation($"从 {ModbusIp}:{ModbusPort} 读取输入寄存器");
                _logger.LogInformation($"从站地址: {ModbusSlaveId}, 起始地址: {startAddress}, 数量: {count}");
                
                // 使用长连接获取Modbus主站
                var master = GetModbusMaster(ModbusIp, ModbusPort);
                if (master == null)
                {
                    _logger.LogError($"无法创建Modbus主站: 无法连接到设备 {ModbusIp}:{ModbusPort}");
                    return new ModbusReadResponse(false, "无法创建Modbus主站: 无法连接到设备", Array.Empty<ushort>());
                }

                // 读取输入寄存器
                var values = await master.ReadInputRegistersAsync((byte)ModbusSlaveId, startAddress, count);
                
                _logger.LogInformation($"读取成功: {string.Join(", ", values)}");
                return new ModbusReadResponse(true, "读取成功", values);
            }
            catch (Exception ex)
            {
                _logger.LogError($"读取失败: {ex.Message}");
                // 发生异常时，移除无效连接
                string connectionKey = $"{ModbusIp}:{ModbusPort}";
                RemoveInvalidConnection(connectionKey);
                return new ModbusReadResponse(false, $"读取失败: {ex.Message}", Array.Empty<ushort>());
            }
        }

        /// <summary>
        /// 与指定IP和端口的Modbus设备建立TCP连接并读取输入寄存器（04功能码）
        /// </summary>
        public async Task<ModbusReadResponse> ReadInputRegistersAsync(string ip, int port, byte slaveId, ushort startAddress, ushort count)
        {
            try
            {
                _logger.LogInformation($"从 {ip}:{port} 读取输入寄存器");
                _logger.LogInformation($"从站地址: {slaveId}, 起始地址: {startAddress}, 数量: {count}");
                
                // 使用长连接获取Modbus主站
                var master = GetModbusMaster(ip, port);
                if (master == null)
                {
                    _logger.LogError($"无法创建Modbus主站: 无法连接到设备 {ip}:{port}");
                    return new ModbusReadResponse(false, "无法创建Modbus主站: 无法连接到设备", Array.Empty<ushort>());
                }

                // 读取输入寄存器
                var values = await master.ReadInputRegistersAsync(slaveId, startAddress, count);
                
                _logger.LogInformation($"读取成功: {string.Join(", ", values)}");
                return new ModbusReadResponse(true, "读取成功", values);
            }
            catch (Exception ex)
            {
                _logger.LogError($"读取失败: {ex.Message}");
                // 发生异常时，移除无效连接
                string connectionKey = $"{ip}:{port}";
                RemoveInvalidConnection(connectionKey);
                return new ModbusReadResponse(false, $"读取失败: {ex.Message}", Array.Empty<ushort>());
            }
        }

        /// <summary>
        /// 通过TCP发送原始485数据（透传模式），使用长连接
        /// </summary>
        public async Task<ModbusResponse> SendRawDataAsync(string ip, int port, byte[] rawData)
        {
            string connectionKey = $"{ip}:{port}";
            TcpClient client = null;
            
            // 最多尝试5次，直到收到返回值
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 发送485数据到 {ip}:{port}");
                    string hexData = BitConverter.ToString(rawData).Replace("-", " ");
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 发送数据内容 (HEX): {hexData}");
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 发送数据长度: {rawData.Length} 字节");
                    
                    // 获取或创建长连接
                    client = GetOrCreateLongConnection(connectionKey, ip, port);
                    
                    // 如果无法创建连接，进行下一次尝试
                    if (client == null)
                    {
                        _logger.LogError($"[尝试 {attempt}/{maxAttempts}] 无法创建长连接到 {ip}:{port}");
                        if (attempt == maxAttempts)
                        {
                            return new ModbusResponse(false, "无法创建连接到设备");
                        }
                        // 短暂延迟后重试
                        int delayMs = 100 * (int)Math.Pow(2, attempt - 1);
                        _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 无法创建连接，{delayMs}ms后重试");
                        await Task.Delay(delayMs);
                        continue;
                    }
                    
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 获取网络流");
                    var stream = client.GetStream();
                    
                    // 清空接收缓冲区，避免读取旧数据
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 清空接收缓冲区");
                    await ClearReceiveBufferAsync(stream);
                    
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 开始发送原始数据");
                    // 发送原始数据
                    await stream.WriteAsync(rawData, 0, rawData.Length);
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 原始数据已写入发送缓冲区");
                    
                    // 强制刷新发送缓冲区，确保数据发送到网络
                    await stream.FlushAsync();
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 发送缓冲区已刷新");
                    
                    // 短暂延迟，给设备时间处理命令
                    await Task.Delay(100);
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 等待设备响应...");
                    
                    // 接收响应
                    byte[] responseData = await ReadModbusResponseAsync(stream, attempt, maxAttempts);
                    
                    if (responseData != null && responseData.Length > 0)
                    {
                        string hexResponse = BitConverter.ToString(responseData).Replace("-", " ");
                        _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 收到响应数据: {hexResponse}");
                        
                        // 尝试解析响应数据的含义（如果是Modbus响应）
                        if (responseData.Length >= 3)
                        {
                            byte slaveId = responseData[0];
                            byte functionCode = responseData[1];
                            _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 响应解析: 从站地址={slaveId:X2}, 功能码={functionCode:X2}");
                        }
                        
                        _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 数据发送成功到 {ip}:{port}，收到响应");
                        var response = new ModbusResponse(true, "发送成功，收到响应");
                        response.ResponseData = responseData;
                        return response;
                    }
                    else
                    {
                        _logger.LogWarning($"[尝试 {attempt}/{maxAttempts}] 未收到响应数据");
                        if (attempt == maxAttempts)
                        {
                            _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 数据发送成功到 {ip}:{port}，但未收到响应");
                            return new ModbusResponse(true, "发送成功，但未收到响应");
                        }
                        // 短暂延迟后重试
                        int delayMs = 100 * (int)Math.Pow(2, attempt - 1);
                        _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 未收到响应，{delayMs}ms后重试");
                        await Task.Delay(delayMs);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[尝试 {attempt}/{maxAttempts}] 发送失败: {ex.Message}");
                    _logger.LogError($"[尝试 {attempt}/{maxAttempts}] 异常详情: {ex}");
                    
                    // 发生异常时，移除无效连接
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 移除无效连接: {connectionKey}");
                    RemoveInvalidConnection(connectionKey);
                    
                    if (attempt == maxAttempts)
                    {
                        return new ModbusResponse(false, $"发送失败: {ex.Message}");
                    }
                    // 短暂延迟后重试
                    int delayMs = 100 * (int)Math.Pow(2, attempt - 1);
                    _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 发送失败，{delayMs}ms后重试");
                    await Task.Delay(delayMs);
                }
            }
            
            return new ModbusResponse(false, "发送失败: 达到最大尝试次数");
        }

        /// <summary>
        /// 清空接收缓冲区
        /// </summary>
        private async Task ClearReceiveBufferAsync(NetworkStream stream)
        {
            try
            {
                // 设置一个短的超时时间，快速清空缓冲区
                byte[] tempBuffer = new byte[1024];
                int totalCleared = 0;
                
                while (stream.DataAvailable)
                {
                    int bytesRead = await stream.ReadAsync(tempBuffer, 0, tempBuffer.Length);
                    totalCleared += bytesRead;
                    if (bytesRead == 0) break;
                }
                
                if (totalCleared > 0)
                {
                    _logger.LogInformation($"清空了 {totalCleared} 字节的旧数据");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"清空缓冲区时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取Modbus响应数据
        /// </summary>
        private async Task<byte[]> ReadModbusResponseAsync(NetworkStream stream, int attempt, int maxAttempts)
        {
            byte[] buffer = new byte[1024];
            List<byte> responseData = new List<byte>();
            
            try
            {
                _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 开始读取响应，超时2秒");
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
                
                // 首先等待数据到达
                DateTime startTime = DateTime.Now;
                while (!stream.DataAvailable)
                {
                    if (DateTime.Now - startTime > TimeSpan.FromSeconds(2))
                    {
                        _logger.LogWarning($"[尝试 {attempt}/{maxAttempts}] 等待数据超时");
                        return null;
                    }
                    await Task.Delay(10);
                }
                
                _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 检测到数据可用，开始读取");
                
                // 读取数据直到没有更多数据或超时
                startTime = DateTime.Now;
                while (DateTime.Now - startTime < TimeSpan.FromSeconds(2))
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        if (bytesRead > 0)
                        {
                            responseData.AddRange(buffer.Take(bytesRead));
                            _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 读取到 {bytesRead} 字节，累计 {responseData.Count} 字节");
                            
                            // 检查是否已读取完整的Modbus响应
                            if (IsCompleteModbusResponse(responseData.ToArray()))
                            {
                                _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 检测到完整的Modbus响应");
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        // 如果没有更多数据，等待一小段时间再检查
                        await Task.Delay(50);
                        
                        // 如果已经读取了一些数据，并且没有新数据到达，认为响应完整
                        if (responseData.Count > 0 && !stream.DataAvailable)
                        {
                            _logger.LogInformation($"[尝试 {attempt}/{maxAttempts}] 没有更多数据，响应接收完成");
                            break;
                        }
                    }
                }
                
                return responseData.ToArray();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"[尝试 {attempt}/{maxAttempts}] 读取响应超时");
                return responseData.Count > 0 ? responseData.ToArray() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[尝试 {attempt}/{maxAttempts}] 读取响应时出错: {ex.Message}");
                return responseData.Count > 0 ? responseData.ToArray() : null;
            }
        }

        /// <summary>
        /// 检查是否是完整的Modbus响应
        /// </summary>
        private bool IsCompleteModbusResponse(byte[] data)
        {
            if (data.Length < 5) return false; // 最小响应长度
            
            byte functionCode = data[1];
            
            // 根据功能码判断响应长度
            switch (functionCode)
            {
                case 0x01: // 读取线圈
                case 0x02: // 读取离散输入
                    if (data.Length < 3) return false;
                    int byteCount1 = data[2];
                    return data.Length >= 3 + byteCount1;
                    
                case 0x03: // 读取保持寄存器
                case 0x04: // 读取输入寄存器
                    if (data.Length < 3) return false;
                    int byteCount2 = data[2];
                    return data.Length >= 3 + byteCount2;
                    
                case 0x05: // 写单个线圈
                case 0x06: // 写单个寄存器
                    return data.Length >= 8; // 请求回显，8字节
                    
                case 0x0F: // 写多个线圈
                case 0x10: // 写多个寄存器
                    return data.Length >= 8; // 8字节响应
                    
                default:
                    // 对于未知功能码，假设响应完整
                    return true;
            }
        }

        /// <summary>
        /// 通过TCP接收原始485数据（透传模式）
        /// </summary>
        public async Task<ModbusReadResponse> ReceiveRawDataAsync(string ip, int port, int bufferSize = 1024)
        {
            try
            {
                _logger.LogInformation($"从 {ip}:{port} 接收485数据");
                
                using var client = new TcpClient();
                // 设置连接超时时间为5秒
                var connectTask = client.ConnectAsync(ip, port);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                {
                    _logger.LogError($"连接超时: 无法连接到设备 {ip}:{port}");
                    return new ModbusReadResponse(false, "连接超时: 无法连接到设备", Array.Empty<ushort>());
                }
                
                using var stream = client.GetStream();
                byte[] buffer = new byte[bufferSize];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                
                _logger.LogInformation($"接收数据长度: {bytesRead} 字节");
                _logger.LogInformation($"接收数据: {BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " ")}");
                
                // 转换为ushort数组返回
                ushort[] result = new ushort[bytesRead / 2];
                for (int i = 0; i < bytesRead / 2; i++)
                {
                    result[i] = (ushort)((buffer[i * 2] << 8) | buffer[i * 2 + 1]);
                }
                
                _logger.LogInformation($"数据接收成功从 {ip}:{port}");
                return new ModbusReadResponse(true, "接收成功", result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"接收失败: {ex.Message}");
                return new ModbusReadResponse(false, $"接收失败: {ex.Message}", Array.Empty<ushort>());
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (_connectionLock)
            {
                foreach (var client in _longConnections.Values)
                {
                    try { client.Dispose(); } catch { }
                }
                _longConnections.Clear();
            }
        }

        /// <summary>
        /// 构建485格式的数据帧（与Rs485Service相同格式）
        /// </summary>
        public byte[] BuildRs485Frame(byte deviceAddress, byte functionCode, byte[] data)
        {
            // 帧格式: [帧头(1)] [设备地址(1)] [功能码(1)] [数据长度(1)] [数据(N)] [CRC(2)] [帧尾(1)]
            int frameLength = 1 + 1 + 1 + 1 + data.Length + 2 + 1; // 帧头+设备地址+功能码+数据长度+数据+CRC+帧尾
            byte[] frame = new byte[frameLength];
            int index = 0;

            // 帧头
            frame[index++] = 0xAA;

            // 设备地址
            frame[index++] = deviceAddress;

            // 功能码
            frame[index++] = functionCode;

            // 数据长度
            frame[index++] = (byte)data.Length;

            // 数据
            Array.Copy(data, 0, frame, index, data.Length);
            index += data.Length;

            // 计算CRC (不包括帧头和帧尾)
            byte[] crcData = new byte[frameLength - 4]; // 排除帧头和帧尾
            Array.Copy(frame, 1, crcData, 0, crcData.Length); // 从设备地址开始
            ushort crc = CalculateCrc16(crcData);

            // 添加CRC (高位在前)
            frame[index++] = (byte)(crc >> 8);
            frame[index++] = (byte)(crc & 0xFF);

            // 帧尾
            frame[index] = 0x55;

            return frame;
        }

        /// <summary>
        /// 计算CRC-16校验值
        /// </summary>
        public ushort CalculateCrc16(byte[] data)
        {
            const ushort CRC16_POLY = 0xA001;
            const ushort CRC16_INIT = 0xFFFF;
            
            ushort crc = CRC16_INIT;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= CRC16_POLY;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }
    }

    // 响应类
    public class ModbusResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public byte[] ResponseData { get; set; }

        public ModbusResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public class ModbusReadResponse : ModbusResponse
    {
        public ushort[] Values { get; set; }

        public ModbusReadResponse(bool success, string message, ushort[] values)
            : base(success, message)
        {
            Values = values;
        }
    }
}
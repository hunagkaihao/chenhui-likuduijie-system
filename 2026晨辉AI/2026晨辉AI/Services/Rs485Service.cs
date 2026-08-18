using System;
using System.IO.Ports;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace _2026晨辉AI.Services
{
    public class Rs485Service
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }

        private SerialPort _serialPort;
        
        // CRC-16参数 (Modbus RTU标准)
        private const ushort CRC16_POLY = 0xA001; // 多项式
        private const ushort CRC16_INIT = 0xFFFF; // 初始值

        // 数据帧格式定义
        private const byte FRAME_HEADER = 0xAA; // 帧头
        private const byte FRAME_TAIL = 0x55; // 帧尾

        public Rs485Service(IConfiguration configuration)
        {
            PortName = configuration["Rs485:PortName"] ?? "COM1";
            BaudRate = int.TryParse(configuration["Rs485:BaudRate"], out var baudRate) ? baudRate : 9600;
            DataBits = int.TryParse(configuration["Rs485:DataBits"], out var dataBits) ? dataBits : 8;
            Parity = Enum.TryParse<Parity>(configuration["Rs485:Parity"], out var parity) ? parity : Parity.None;
            StopBits = Enum.TryParse<StopBits>(configuration["Rs485:StopBits"], out var stopBits) ? stopBits : StopBits.One;
            ReadTimeout = int.TryParse(configuration["Rs485:ReadTimeout"], out var readTimeout) ? readTimeout : 1000;
            WriteTimeout = int.TryParse(configuration["Rs485:WriteTimeout"], out var writeTimeout) ? writeTimeout : 1000;

            InitializeSerialPort();
        }

        private void InitializeSerialPort()
        {
            _serialPort = new SerialPort
            {
                PortName = PortName,
                BaudRate = BaudRate,
                DataBits = DataBits,
                Parity = Parity,
                StopBits = StopBits,
                ReadTimeout = ReadTimeout,
                WriteTimeout = WriteTimeout
            };
        }

        public bool Open()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开串口失败: {ex.Message}");
                return false;
            }
        }

        public void Close()
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭串口失败: {ex.Message}");
            }
        }

        public async Task<Rs485Response> SendDataAsync(byte[] data)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    if (!Open())
                    {
                        return new Rs485Response(false, "串口未打开");
                    }
                }

                _serialPort.Write(data, 0, data.Length);
                return new Rs485Response(true, "发送成功");
            }
            catch (Exception ex)
            {
                return new Rs485Response(false, $"发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送带CRC校验的数据（带重试机制）
        /// </summary>
        public async Task<Rs485Response> SendDataWithCrcAsync(byte deviceAddress, byte functionCode, byte[] data, int retryCount = 3)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    // 构建带CRC校验的数据帧
                    byte[] frame = BuildFrame(deviceAddress, functionCode, data);

                    // 发送数据帧
                    if (!_serialPort.IsOpen)
                    {
                        if (!Open())
                        {
                            Log.Error("串口未打开，尝试第 {RetryCount} 次发送失败", i + 1);
                            continue;
                        }
                    }

                    _serialPort.Write(frame, 0, frame.Length);
                    Log.Information("发送数据成功: 设备地址={DeviceAddress}, 功能码={FunctionCode}, 数据长度={DataLength}", deviceAddress, functionCode, data.Length);
                    return new Rs485Response(true, "发送成功");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "发送数据失败（第 {RetryCount} 次尝试）: {ErrorMessage}", i + 1, ex.Message);
                    if (i == retryCount - 1)
                    {
                        return new Rs485Response(false, $"发送失败: {ex.Message}");
                    }
                    // 等待一段时间后重试
                    await Task.Delay(100);
                }
            }
            return new Rs485Response(false, "发送失败: 达到最大重试次数");
        }

        public async Task<Rs485ReadResponse> ReadDataAsync(int count)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    if (!Open())
                    {
                        return new Rs485ReadResponse(false, "串口未打开", Array.Empty<byte>());
                    }
                }

                byte[] buffer = new byte[count];
                int bytesRead = _serialPort.Read(buffer, 0, count);
                byte[] actualData = new byte[bytesRead];
                Array.Copy(buffer, actualData, bytesRead);

                return new Rs485ReadResponse(true, "读取成功", actualData);
            }
            catch (Exception ex)
            {
                return new Rs485ReadResponse(false, $"读取失败: {ex.Message}", Array.Empty<byte>());
            }
        }

        /// <summary>
        /// 读取并验证带CRC校验的数据（带重试机制）
        /// </summary>
        public async Task<Rs485ReadResponse> ReadDataWithCrcAsync(int maxCount, int retryCount = 3)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    if (!_serialPort.IsOpen)
                    {
                        if (!Open())
                        {
                            Log.Error("串口未打开，尝试第 {RetryCount} 次读取失败", i + 1);
                            continue;
                        }
                    }

                    // 读取数据
                    byte[] buffer = new byte[maxCount];
                    int bytesRead = _serialPort.Read(buffer, 0, maxCount);
                    byte[] actualData = new byte[bytesRead];
                    Array.Copy(buffer, actualData, bytesRead);

                    // 解析数据帧
                    var (isValid, deviceAddress, functionCode, data) = ParseFrame(actualData);

                    if (!isValid)
                    {
                        Log.Warning("数据帧无效或CRC校验失败，尝试第 {RetryCount} 次读取", i + 1);
                        if (i == retryCount - 1)
                        {
                            return new Rs485ReadResponse(false, "数据帧无效或CRC校验失败", Array.Empty<byte>());
                        }
                        continue;
                    }

                    Log.Information("读取数据成功: 设备地址={DeviceAddress}, 功能码={FunctionCode}, 数据长度={DataLength}", deviceAddress, functionCode, data.Length);
                    // 返回解析后的数据
                    return new Rs485ReadResponse(true, "读取成功", data);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "读取数据失败（第 {RetryCount} 次尝试）: {ErrorMessage}", i + 1, ex.Message);
                    if (i == retryCount - 1)
                    {
                        return new Rs485ReadResponse(false, $"读取失败: {ex.Message}", Array.Empty<byte>());
                    }
                    // 等待一段时间后重试
                    await Task.Delay(100);
                }
            }
            return new Rs485ReadResponse(false, "读取失败: 达到最大重试次数", Array.Empty<byte>());
        }

        /// <summary>
        /// 计算CRC-16校验值
        /// </summary>
        public ushort CalculateCrc16(byte[] data)
        {
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

        /// <summary>
        /// 验证CRC-16校验值
        /// </summary>
        public bool VerifyCrc16(byte[] data, ushort expectedCrc)
        {
            ushort calculatedCrc = CalculateCrc16(data);
            return calculatedCrc == expectedCrc;
        }

        /// <summary>
        /// 从数据末尾提取CRC值
        /// </summary>
        public ushort ExtractCrcFromData(byte[] data)
        {
            if (data.Length < 2)
            {
                throw new ArgumentException("数据长度不足，无法提取CRC值");
            }
            return (ushort)((data[data.Length - 2] << 8) | data[data.Length - 1]);
        }

        /// <summary>
        /// 移除数据末尾的CRC值
        /// </summary>
        public byte[] RemoveCrcFromData(byte[] data)
        {
            if (data.Length < 2)
            {
                throw new ArgumentException("数据长度不足，无法移除CRC值");
            }
            byte[] result = new byte[data.Length - 2];
            Array.Copy(data, 0, result, 0, data.Length - 2);
            return result;
        }

        /// <summary>
        /// 构建包含CRC校验的数据帧
        /// 帧格式: [帧头(1)] [设备地址(1)] [功能码(1)] [数据长度(1)] [数据(N)] [CRC(2)] [帧尾(1)]
        /// </summary>
        public byte[] BuildFrame(byte deviceAddress, byte functionCode, byte[] data)
        {
            int frameLength = 1 + 1 + 1 + 1 + data.Length + 2 + 1; // 帧头+设备地址+功能码+数据长度+数据+CRC+帧尾
            byte[] frame = new byte[frameLength];
            int index = 0;

            // 帧头
            frame[index++] = FRAME_HEADER;

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
            frame[index] = FRAME_TAIL;

            return frame;
        }

        /// <summary>
        /// 解析接收到的数据帧
        /// </summary>
        public (bool isValid, byte deviceAddress, byte functionCode, byte[] data) ParseFrame(byte[] rawData)
        {
            // 检查帧长度
            if (rawData.Length < 8) // 最小帧长度: 帧头(1) + 设备地址(1) + 功能码(1) + 数据长度(1) + 数据(0) + CRC(2) + 帧尾(1)
            {
                return (false, 0, 0, Array.Empty<byte>());
            }

            // 检查帧头和帧尾
            if (rawData[0] != FRAME_HEADER || rawData[rawData.Length - 1] != FRAME_TAIL)
            {
                return (false, 0, 0, Array.Empty<byte>());
            }

            // 提取数据长度
            byte dataLength = rawData[3];

            // 检查数据长度是否正确
            if (rawData.Length != 1 + 1 + 1 + 1 + dataLength + 2 + 1)
            {
                return (false, 0, 0, Array.Empty<byte>());
            }

            // 验证CRC
            byte[] crcData = new byte[rawData.Length - 4]; // 排除帧头和帧尾
            Array.Copy(rawData, 1, crcData, 0, crcData.Length); // 从设备地址开始
            ushort expectedCrc = ExtractCrcFromData(crcData);
            byte[] dataWithoutCrc = RemoveCrcFromData(crcData);
            bool crcValid = VerifyCrc16(dataWithoutCrc, expectedCrc);

            if (!crcValid)
            {
                return (false, 0, 0, Array.Empty<byte>());
            }

            // 提取设备地址和功能码
            byte deviceAddress = rawData[1];
            byte functionCode = rawData[2];

            // 提取数据
            byte[] data = new byte[dataLength];
            Array.Copy(rawData, 4, data, 0, dataLength);

            return (true, deviceAddress, functionCode, data);
        }
    }

    public class Rs485Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public Rs485Response(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public class Rs485ReadResponse : Rs485Response
    {
        public byte[] Data { get; set; }

        public Rs485ReadResponse(bool success, string message, byte[] data)
            : base(success, message)
        {
            Data = data;
        }
    }
}

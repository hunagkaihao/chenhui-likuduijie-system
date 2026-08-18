using Microsoft.AspNetCore.Mvc;
using _2026晨辉AI.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModbusController : ControllerBase
    {
        private readonly ModbusService _modbusService;
        private readonly ILogger<ModbusController> _logger;

        public ModbusController(ModbusService modbusService, ILogger<ModbusController> logger)
        {
            _modbusService = modbusService;
            _logger = logger;
        }

        /// <summary>
        /// 发送Modbus报文（写入保持寄存器）
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ModbusSendRequest request)
        {
            var response = await _modbusService.SendMessageAsync(request.StartAddress, request.Values);
            return Ok(response);
        }

        /// <summary>
        /// 发送Modbus报文（写入单个保持寄存器）
        /// </summary>
        [HttpPost("write-single")]
        public async Task<IActionResult> WriteSingleRegister([FromBody] ModbusWriteSingleRequest request)
        {
            var response = await _modbusService.WriteSingleRegisterAsync(request.Address, request.Value);
            return Ok(response);
        }

        /// <summary>
        /// 发送Modbus报文到指定IP和端口（写入单个保持寄存器）
        /// </summary>
        [HttpPost("write-single-remote")]
        public async Task<IActionResult> WriteSingleRegisterRemote([FromBody] ModbusWriteSingleRemoteRequest request)
        {
            var response = await _modbusService.WriteSingleRegisterAsync(
                request.Ip, 
                request.Port, 
                request.SlaveId, 
                request.Address, 
                request.Value
            );
            return Ok(response);
        }

        /// <summary>
        /// 发送Modbus报文（写入线圈）
        /// </summary>
        [HttpPost("send-coil")]
        public async Task<IActionResult> SendCoilMessage([FromBody] ModbusSendCoilRequest request)
        {
            var response = await _modbusService.SendCoilMessageAsync(request.StartAddress, request.Values);
            return Ok(response);
        }

        /// <summary>
        /// 读取Modbus保持寄存器
        /// </summary>
        [HttpGet("read")]
        public async Task<IActionResult> ReadRegisters([FromQuery] ushort startAddress, [FromQuery] ushort count)
        {
            var response = await _modbusService.ReadRegistersAsync(startAddress, count);
            return Ok(response);
        }

        /// <summary>
        /// 读取Modbus输入寄存器（04功能码）
        /// </summary>
        [HttpGet("read-input")]
        public async Task<IActionResult> ReadInputRegisters([FromQuery] ushort startAddress, [FromQuery] ushort count)
        {
            var response = await _modbusService.ReadInputRegistersAsync(startAddress, count);
            return Ok(response);
        }

        /// <summary>
        /// 读取指定IP和端口的Modbus输入寄存器（04功能码）
        /// </summary>
        [HttpPost("read-input-remote")]
        public async Task<IActionResult> ReadInputRegistersRemote([FromBody] ModbusReadInputRemoteRequest request)
        {
            var response = await _modbusService.ReadInputRegistersAsync(
                request.Ip, 
                request.Port, 
                request.SlaveId, 
                request.StartAddress, 
                request.Count
            );
            return Ok(response);
        }

        /// <summary>
        /// 通过TCP发送原始485数据（透传模式）
        /// </summary>
        [HttpPost("send-raw")]
        public async Task<IActionResult> SendRawData([FromBody] ModbusSendRawRequest request)
        {
            var response = await _modbusService.SendRawDataAsync(
                request.Ip, 
                request.Port, 
                request.Data
            );
            return Ok(response);
        }

        /// <summary>
        /// 通过TCP接收原始485数据（透传模式）
        /// </summary>
        [HttpPost("receive-raw")]
        public async Task<IActionResult> ReceiveRawData([FromBody] ModbusReceiveRawRequest request)
        {
            var response = await _modbusService.ReceiveRawDataAsync(
                request.Ip, 
                request.Port, 
                request.BufferSize
            );
            return Ok(response);
        }

        /// <summary>
        /// 构建485格式的数据帧
        /// </summary>
        [HttpPost("build-frame")]
        public IActionResult BuildFrame()
        {
            try
            {
                using (var reader = new System.IO.StreamReader(Request.Body))
                {
                    var json = reader.ReadToEnd();
                    dynamic request = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                    
                    byte deviceAddress = Convert.ToByte(request.deviceAddress);
                    byte functionCode = Convert.ToByte(request.functionCode);
                    var dataArray = (JsonElement)request.data;
                    byte[] data = new byte[dataArray.GetArrayLength()];
                    for (int i = 0; i < dataArray.GetArrayLength(); i++)
                    {
                        data[i] = (byte)dataArray[i].GetInt32();
                    }
                    
                    byte[] frame = _modbusService.BuildRs485Frame(deviceAddress, functionCode, data);
                    return Ok(new { frame = frame, frameHex = BitConverter.ToString(frame).Replace("-", " ") });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// 获取Modbus连接配置
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var config = new
            {
                Ip = _modbusService.ModbusIp,
                Port = _modbusService.ModbusPort,
                SlaveId = _modbusService.ModbusSlaveId
            };
            return Ok(config);
        }

        /// <summary>
        /// 测试API
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "API is working" });
        }

        /// <summary>
        /// 闭合继电器（一号位）
        /// </summary>
        [HttpPost("relay/close")]
        public async Task<IActionResult> CloseRelay([FromBody] ModbusRemoteRequest request)
        {
            // 闭合继电器命令: 01 06 00 00 00 01 48 0A
            byte[] command = new byte[] { 0x01, 0x06, 0x00, 0x00, 0x00, 0x01, 0x48, 0x0A };
            string commandHex = BitConverter.ToString(command).Replace("-", " ");
            _logger.LogInformation($"发送闭合继电器命令到 {request.Ip}:{request.Port}，命令: {commandHex}");
            var response = await _modbusService.SendRawDataAsync(request.Ip, request.Port, command);
            _logger.LogInformation($"闭合继电器命令响应: {response.Message}");
            return Ok(response);
        }

        /// <summary>
        /// 断开继电器（一号位）
        /// </summary>
        [HttpPost("relay/open")]
        public async Task<IActionResult> OpenRelay([FromBody] ModbusRemoteRequest request)
        {
            // 断开继电器命令: 01 05 00 00 00 00 CD CA
            byte[] command = new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0xCD, 0xCA };
            string commandHex = BitConverter.ToString(command).Replace("-", " ");
            _logger.LogInformation($"发送断开继电器命令到 {request.Ip}:{request.Port}，命令: {commandHex}");
            var response = await _modbusService.SendRawDataAsync(request.Ip, request.Port, command);
            _logger.LogInformation($"断开继电器命令响应: {response.Message}");
            return Ok(response);
        }

        /// <summary>
        /// 读取继电器状态（一号位）
        /// </summary>
        [HttpPost("relay/status")]
        public async Task<IActionResult> GetRelayStatus([FromBody] ModbusRemoteRequest request)
        {
            // 读取状态命令: 01 04 00 00 00 01 31 CA
            byte[] command = new byte[] { 0x01, 0x04, 0x00, 0x00, 0x00, 0x01, 0x31, 0xCA };
            string commandHex = BitConverter.ToString(command).Replace("-", " ");
            _logger.LogInformation($"发送读取继电器状态命令到 {request.Ip}:{request.Port}，命令: {commandHex}");
            var response = await _modbusService.SendRawDataAsync(request.Ip, request.Port, command);
            _logger.LogInformation($"读取继电器状态命令响应: {response.Message}");
            return Ok(response);
        }
    }

    // 请求类
    public class ModbusSendRequest
    {
        public ushort StartAddress { get; set; }
        public ushort[] Values { get; set; }
    }

    public class ModbusSendCoilRequest
    {
        public ushort StartAddress { get; set; }
        public bool[] Values { get; set; }
    }

    public class ModbusWriteSingleRequest
    {
        public ushort Address { get; set; }
        public ushort Value { get; set; }
    }

    public class ModbusWriteSingleRemoteRequest
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public byte SlaveId { get; set; }
        public ushort Address { get; set; }
        public ushort Value { get; set; }
    }

    public class ModbusReadInputRemoteRequest
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public byte SlaveId { get; set; }
        public ushort StartAddress { get; set; }
        public ushort Count { get; set; }
    }

    public class ModbusSendRawRequest
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public byte[] Data { get; set; }
    }

    public class ModbusReceiveRawRequest
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public int BufferSize { get; set; } = 1024;
    }

    public class ModbusBuildFrameRequest
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class ModbusRemoteRequest
    {
        public string Ip { get; set; }
        public int Port { get; set; }
    }
}
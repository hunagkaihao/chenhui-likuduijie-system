using Microsoft.AspNetCore.Mvc;
using _2026晨辉AI.Services;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Rs485Controller : ControllerBase
    {
        private readonly Rs485Service _rs485Service;

        public Rs485Controller(Rs485Service rs485Service)
        {
            _rs485Service = rs485Service;
        }

        /// <summary>
        /// 发送485数据（带CRC校验）
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendData([FromBody] Rs485SendRequest request)
        {
            var response = await _rs485Service.SendDataWithCrcAsync(
                request.DeviceAddress, 
                request.FunctionCode, 
                request.Data,
                request.RetryCount
            );
            return Ok(response);
        }

        /// <summary>
        /// 读取485数据（带CRC校验）
        /// </summary>
        [HttpGet("read")]
        public async Task<IActionResult> ReadData([FromQuery] int maxCount = 100, [FromQuery] int retryCount = 3)
        {
            var response = await _rs485Service.ReadDataWithCrcAsync(maxCount, retryCount);
            return Ok(response);
        }

        /// <summary>
        /// 打开485串口
        /// </summary>
        [HttpPost("open")]
        public IActionResult OpenPort()
        {
            bool result = _rs485Service.Open();
            return Ok(new { success = result, message = result ? "串口打开成功" : "串口打开失败" });
        }

        /// <summary>
        /// 关闭485串口
        /// </summary>
        [HttpPost("close")]
        public IActionResult ClosePort()
        {
            _rs485Service.Close();
            return Ok(new { success = true, message = "串口关闭成功" });
        }

        /// <summary>
        /// 获取485串口配置
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var config = new
            {
                PortName = _rs485Service.PortName,
                BaudRate = _rs485Service.BaudRate,
                DataBits = _rs485Service.DataBits,
                Parity = _rs485Service.Parity.ToString(),
                StopBits = _rs485Service.StopBits.ToString(),
                ReadTimeout = _rs485Service.ReadTimeout,
                WriteTimeout = _rs485Service.WriteTimeout
            };
            return Ok(config);
        }

        /// <summary>
        /// 控制第一通道继电器吸合
        /// </summary>
        [HttpPost("control-relay")]
        public async Task<IActionResult> ControlRelay([FromBody] RelayControlRequest request)
        {
            // 构建控制继电器的数据
            byte[] data = new byte[4];
            data[0] = (byte)(request.Address >> 8);  // 寄存器地址高位
            data[1] = (byte)(request.Address & 0xFF);  // 寄存器地址低位
            data[2] = (byte)(request.Value >> 8);  // 寄存器值高位
            data[3] = (byte)(request.Value & 0xFF);  // 寄存器值低位

            var response = await _rs485Service.SendDataWithCrcAsync(
                deviceAddress: 0x01,  // 从站地址
                functionCode: 0x06,  // 功能码（06为写单个保持寄存器）
                data: data
            );
            return Ok(response);
        }

        /// <summary>
        /// 读取通道数据
        /// </summary>
        [HttpPost("read-channel")]
        public async Task<IActionResult> ReadChannel([FromBody] ChannelReadRequest request)
        {
            // 构建读取通道的数据
            byte[] data = new byte[4];
            data[0] = (byte)(request.StartAddress >> 8);  // 起始地址高位
            data[1] = (byte)(request.StartAddress & 0xFF);  // 起始地址低位
            data[2] = (byte)(request.Count >> 8);  // 读取个数高位
            data[3] = (byte)(request.Count & 0xFF);  // 读取个数低位

            var response = await _rs485Service.SendDataWithCrcAsync(
                deviceAddress: 0x01,  // 从站地址
                functionCode: 0x04,  // 功能码（04为读取输入寄存器）
                data: data
            );

            if (response.Success)
            {
                // 读取响应数据
                var readResponse = await _rs485Service.ReadDataWithCrcAsync(100);
                return Ok(readResponse);
            }
            else
            {
                return Ok(response);
            }
        }
    }

    // 请求类
    public class Rs485SendRequest
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte[] Data { get; set; }
        public int RetryCount { get; set; } = 3;
    }

    public class RelayControlRequest
    {
        public ushort Address { get; set; }  // 继电器地址
        public ushort Value { get; set; }  // 控制值（1为吸合，0为释放）
    }

    public class ChannelReadRequest
    {
        public ushort StartAddress { get; set; }  // 起始地址
        public ushort Count { get; set; }  // 读取个数
    }
}

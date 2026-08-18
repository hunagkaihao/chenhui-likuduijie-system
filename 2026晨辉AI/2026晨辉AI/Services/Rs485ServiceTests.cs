using System;
using System.IO.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;

namespace _2026晨辉AI.Services.Tests
{
    public class Rs485ServiceTests
    {
        private Rs485Service _rs485Service;

        public Rs485ServiceTests()
        {
            // 创建测试配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _rs485Service = new Rs485Service(configuration);
        }

        public void TestCrc16Calculation()
        {
            Console.WriteLine("=== 测试CRC-16计算 ===");

            // 测试数据1: 简单的字节数组
            byte[] testData1 = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02 };
            ushort crc1 = _rs485Service.CalculateCrc16(testData1);
            Console.WriteLine($"测试数据1: {BitConverter.ToString(testData1)}");
            Console.WriteLine($"计算的CRC: 0x{crc1:X4}");
            Console.WriteLine();

            // 测试数据2: 更长的字节数组
            byte[] testData2 = { 0x01, 0x06, 0x00, 0x01, 0x00, 0x0A };
            ushort crc2 = _rs485Service.CalculateCrc16(testData2);
            Console.WriteLine($"测试数据2: {BitConverter.ToString(testData2)}");
            Console.WriteLine($"计算的CRC: 0x{crc2:X4}");
            Console.WriteLine();

            // 测试数据3: 单个字节
            byte[] testData3 = { 0x01 };
            ushort crc3 = _rs485Service.CalculateCrc16(testData3);
            Console.WriteLine($"测试数据3: {BitConverter.ToString(testData3)}");
            Console.WriteLine($"计算的CRC: 0x{crc3:X4}");
            Console.WriteLine();

            Console.WriteLine("CRC-16计算测试完成");
            Console.WriteLine("===================");
        }

        public void TestFrameBuilding()
        {
            Console.WriteLine("=== 测试数据帧构建 ===");

            // 测试数据
            byte deviceAddress = 0x01;
            byte functionCode = 0x03;
            byte[] data = { 0x00, 0x00, 0x00, 0x02 };

            // 构建数据帧
            byte[] frame = _rs485Service.BuildFrame(deviceAddress, functionCode, data);

            Console.WriteLine($"设备地址: 0x{deviceAddress:X2}");
            Console.WriteLine($"功能码: 0x{functionCode:X2}");
            Console.WriteLine($"数据: {BitConverter.ToString(data)}");
            Console.WriteLine($"构建的帧: {BitConverter.ToString(frame)}");
            Console.WriteLine($"帧长度: {frame.Length}");
            Console.WriteLine();

            // 验证帧格式
            Console.WriteLine("验证帧格式:");
            Console.WriteLine($"帧头: 0x{frame[0]:X2} (应为0xAA)");
            Console.WriteLine($"设备地址: 0x{frame[1]:X2} (应为0x01)");
            Console.WriteLine($"功能码: 0x{frame[2]:X2} (应为0x03)");
            Console.WriteLine($"数据长度: 0x{frame[3]:X2} (应为0x04)");
            Console.WriteLine($"数据部分: {BitConverter.ToString(frame, 4, data.Length)}");
            Console.WriteLine($"CRC高字节: 0x{frame[4 + data.Length]:X2}");
            Console.WriteLine($"CRC低字节: 0x{frame[5 + data.Length]:X2}");
            Console.WriteLine($"帧尾: 0x{frame[frame.Length - 1]:X2} (应为0x55)");
            Console.WriteLine();

            Console.WriteLine("数据帧构建测试完成");
            Console.WriteLine("===================");
        }

        public void TestFrameParsing()
        {
            Console.WriteLine("=== 测试数据帧解析 ===");

            // 构建一个测试帧
            byte deviceAddress = 0x01;
            byte functionCode = 0x03;
            byte[] data = { 0x00, 0x00, 0x00, 0x02 };
            byte[] frame = _rs485Service.BuildFrame(deviceAddress, functionCode, data);

            Console.WriteLine($"测试帧: {BitConverter.ToString(frame)}");
            Console.WriteLine();

            // 解析帧
            var (isValid, parsedDeviceAddress, parsedFunctionCode, parsedData) = _rs485Service.ParseFrame(frame);

            Console.WriteLine($"解析结果:");
            Console.WriteLine($"帧有效: {isValid}");
            Console.WriteLine($"设备地址: 0x{parsedDeviceAddress:X2} (应为0x01)");
            Console.WriteLine($"功能码: 0x{parsedFunctionCode:X2} (应为0x03)");
            Console.WriteLine($"数据: {BitConverter.ToString(parsedData)}");
            Console.WriteLine();

            // 测试CRC校验失败的情况
            Console.WriteLine("测试CRC校验失败的情况:");
            byte[] corruptedFrame = (byte[])frame.Clone();
            // 篡改数据
            corruptedFrame[4] ^= 0x01; // 修改数据部分
            var (isValidCorrupted, _, _, _) = _rs485Service.ParseFrame(corruptedFrame);
            Console.WriteLine($"篡改后的帧: {BitConverter.ToString(corruptedFrame)}");
            Console.WriteLine($"帧有效: {isValidCorrupted} (应为false)");
            Console.WriteLine();

            Console.WriteLine("数据帧解析测试完成");
            Console.WriteLine("===================");
        }

        public void RunAllTests()
        {
            Console.WriteLine("开始运行所有测试...");
            Console.WriteLine("============================");

            TestCrc16Calculation();
            Console.WriteLine();

            TestFrameBuilding();
            Console.WriteLine();

            TestFrameParsing();
            Console.WriteLine();

            Console.WriteLine("所有测试运行完成!");
            Console.WriteLine("============================");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _2026晨辉AI.Services
{
    public class RcsApiManager
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RcsApiManager> _logger;
        public string CMSServer { get; set; }
        public string AGVEnable { get; set; }

        public RcsApiManager(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<RcsApiManager> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            CMSServer = configuration["RCS:CMSServer"] ?? "http://localhost:8080";
            AGVEnable = configuration["RCS:AGVEnable"] ?? "true";
        }

        /// <summary>
        /// 创建AGV任务
        /// </summary>
        public async Task<ResultAgvTaskDto> CreateAgvTaskAsync(string reqCode, string taskTyp, string ctnrTyp, string[] userCallCodePath, string taskCode, string boxCode, string agvCode)
        {
            if (AGVEnable.Equals("true"))
            {
                GenAgvTaskDto genAgvTaskDto = new GenAgvTaskDto(reqCode, taskTyp, ctnrTyp, taskCode, userCallCodePath, boxCode, agvCode);
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonSerializer.Serialize(genAgvTaskDto), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{CMSServer}/rcms/services/rest/hikRpcService/genAgvSchedulingTask", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"RCS 创建任务响应: {responseContent}");
                    var result = JsonSerializer.Deserialize<ResultAgvTaskDto>(responseContent);
                    if (result == null)
                    {
                        _logger.LogError($"RCS 创建任务返回空结果，原始响应: {responseContent}");
                        return new ResultAgvTaskDto("1", "RCS返回数据为空", reqCode, "");
                    }
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"RCS API错误: {response.StatusCode}, 内容: {errorContent}");
                    return new ResultAgvTaskDto("1", $"失败: {response.StatusCode} - {errorContent}", reqCode, "");
                }
            }
            else
            {
                return new ResultAgvTaskDto("0", "成功", reqCode, "");
            }
        }

        /// <summary>
        /// 取消AGV任务
        /// </summary>
        public async Task<ResultAgvTaskDto> CancelTaskAsync(string reqCode, string taskCode)
        {
            if (AGVEnable.Equals("true"))
            {
                CancelAgvTaskDto cancelAgvTaskDto = new CancelAgvTaskDto(reqCode, taskCode);
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonSerializer.Serialize(cancelAgvTaskDto), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{CMSServer}/rcms/services/rest/hikRpcService/cancelTask", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ResultAgvTaskDto>(responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"RCS API错误: {response.StatusCode}, 内容: {errorContent}");
                    return new ResultAgvTaskDto("1", $"失败: {response.StatusCode} - {errorContent}", reqCode, "");
                }
            }
            else
            {
                return new ResultAgvTaskDto("0", "成功", reqCode, "");
            }
        }

        /// <summary>
        /// 查询AGV任务状态
        /// </summary>
        public async Task<ResultAgvTaskStatusDto> FindTaskSatusAsync(string reqCode, List<string> taskCodes)
        {
            GetAgvTaskStatusDto getAgvTaskStatusDto = new GetAgvTaskStatusDto(reqCode, taskCodes);
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(getAgvTaskStatusDto), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{CMSServer}/rcms/services/rest/hikRpcService/queryTaskStatus", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ResultAgvTaskStatusDto>(responseContent);
            }
            else
            {
                return new ResultAgvTaskStatusDto("1", "失败", reqCode, new List<AgvTaskStatusDto>());
            }
        }

        /// <summary>
        /// 查询AGV状态
        /// </summary>
        public async Task<ResultAgvStatusDto> QueryAgvStatusAsync(string reqCode, string mapCode)
        {
            if (AGVEnable.Equals("true"))
            {
                QueryAgvStatusDto queryAgvStatusDto = new QueryAgvStatusDto(reqCode, mapCode);
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonSerializer.Serialize(queryAgvStatusDto), System.Text.Encoding.UTF8, "application/json");
                // 使用固定的API地址
                var response = await client.PostAsync("http://192.168.4.3:8181/rcms-dps/rest/queryAgvStatus", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ResultAgvStatusDto>(responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"RCS API错误: {response.StatusCode}, 内容: {errorContent}");
                    return new ResultAgvStatusDto("1", $"失败: {response.StatusCode} - {errorContent}", reqCode);
                }
            }
            else
            {
                return new ResultAgvStatusDto("0", "成功", reqCode);
            }
        }

        /// <summary>
        /// 继续执行AGV任务
        /// </summary>
        public async Task<ResultAgvTaskDto> ContinueTaskAsync(string reqCode, string taskCode)
        {
            if (AGVEnable.Equals("true"))
            {
                ContinueAgvTaskDto continueAgvTaskDto = new ContinueAgvTaskDto(reqCode, taskCode);
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonSerializer.Serialize(continueAgvTaskDto), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{CMSServer}/rcms/services/rest/hikRpcService/continueTask", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ResultAgvTaskDto>(responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"RCS API错误: {response.StatusCode}, 内容: {errorContent}");
                    return new ResultAgvTaskDto("1", $"失败: {response.StatusCode} - {errorContent}", reqCode, "");
                }
            }
            else
            {
                return new ResultAgvTaskDto("0", "成功", reqCode, "");
            }
        }
    }

    public class ContinueAgvTaskDto
    {
        public ContinueAgvTaskDto(string reqCode, string taskCode)
        {
            this.reqCode = reqCode;
            this.taskCode = taskCode;
        }
        public string reqCode { get; set; }
        public string taskCode { get; set; }
    }

    // DTO classes
    public class GenAgvTaskDto
    {
        public string reqCode { get; set; }
        public string taskTyp { get; set; }
        public string ctnrTyp { get; set; }
        public string taskCode { get; set; }
        public string[] userCallCodePath { get; set; }
        public string boxCode { get; set; }
        public string agvCode { get; set; }

        public GenAgvTaskDto(string reqCode, string taskTyp, string ctnrTyp, string taskCode, string[] userCallCodePath, string boxCode, string agvCode)
        {
            this.reqCode = reqCode;
            this.taskTyp = taskTyp;
            this.ctnrTyp = ctnrTyp;
            this.taskCode = taskCode;
            this.userCallCodePath = userCallCodePath;
            this.boxCode = boxCode;
            this.agvCode = agvCode;
        }
    }

    public class CancelAgvTaskDto
    {
        public string reqCode { get; set; }
        public string taskCode { get; set; }

        public CancelAgvTaskDto(string reqCode, string taskCode)
        {
            this.reqCode = reqCode;
            this.taskCode = taskCode;
        }
    }

    public class GetAgvTaskStatusDto
    {
        public string reqCode { get; set; }
        public List<string> taskCodes { get; set; }

        public GetAgvTaskStatusDto(string reqCode, List<string> taskCodes)
        {
            this.reqCode = reqCode;
            this.taskCodes = taskCodes;
        }
    }

    public class QueryAgvStatusDto
    {
        public QueryAgvStatusDto(string reqCode, string mapCode)
        {
            this.reqCode = reqCode;
            this.mapcode = mapCode;
        }
        /// <summary>
        /// 任务请求编号，唯一
        /// </summary>
        public string reqCode { get; set; }
        /// <summary>
        /// 地图编码
        /// </summary>
        public string mapcode { get; set; }
    }

    public class ResultAgvStatusListDto
    {
        /// <summary>
        /// 电量
        /// </summary>
        public string battery { get; set; }
        /// <summary>
        /// 排他类型
        /// </summary>
        public string exclType { get; set; }
        /// <summary>
        /// 地图编码
        /// </summary>
        public string mapCode { get; set; }
        /// <summary>
        /// 是否在线
        /// </summary>
        public bool online { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public List<string> path { get; set; }
        /// <summary>
        /// 托盘编码
        /// </summary>
        public string podCode { get; set; }
        /// <summary>
        /// 托盘方向
        /// </summary>
        public string podDir { get; set; }
        /// <summary>
        /// X坐标
        /// </summary>
        public string posX { get; set; }
        /// <summary>
        /// Y坐标
        /// </summary>
        public string posY { get; set; }
        /// <summary>
        /// 机器人编码
        /// </summary>
        public string robotCode { get; set; }
        /// <summary>
        /// 机器人方向
        /// </summary>
        public string robotDir { get; set; }
        /// <summary>
        /// 机器人IP
        /// </summary>
        public string robotIp { get; set; }
        /// <summary>
        /// 速度
        /// </summary>
        public string speed { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 是否停止
        /// </summary>
        public string stop { get; set; }
        /// <summary>
        /// 时间戳
        /// </summary>
        public long timestamp { get; set; }
    }

    public class ResultAgvTaskDto
    {
        public string code { get; set; }
        public string message { get; set; }
        public string reqCode { get; set; }
        public string taskCode { get; set; }

        public ResultAgvTaskDto(string code, string message, string reqCode, string taskCode)
        {
            this.code = code;
            this.message = message;
            this.reqCode = reqCode;
            this.taskCode = taskCode;
        }
    }

    public class ResultAgvTaskStatusDto
    {
        public string code { get; set; }
        public string message { get; set; }
        public string reqCode { get; set; }
        public List<AgvTaskStatusDto> taskStatusList { get; set; }

        public ResultAgvTaskStatusDto(string code, string message, string reqCode, List<AgvTaskStatusDto> taskStatusList)
        {
            this.code = code;
            this.message = message;
            this.reqCode = reqCode;
            this.taskStatusList = taskStatusList;
        }
    }

    public class AgvTaskStatusDto
    {
        public string taskCode { get; set; }
        public string status { get; set; }
        public string message { get; set; }
    }

    public class ResultAgvStatusDto
    {
        public ResultAgvStatusDto()
        {

        }
        public ResultAgvStatusDto(string code, string message, string reqCode)
        {
            this.code = code;
            this.message = message;
            this.reqCode = reqCode;
        }
        /// <summary>
        /// 返回码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 返回消息
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 请求编号
        /// </summary>
        public string reqCode { get; set; }
        /// <summary>
        /// 自定义返回数据
        /// </summary>
        public List<ResultAgvStatusListDto> data { get; set; }
        /// <summary>
        /// 是否中断
        /// </summary>
        public bool interrupt { get; set; }
        /// <summary>
        /// 错误代码
        /// </summary>
        public string msgErrCode { get; set; }
    }
}
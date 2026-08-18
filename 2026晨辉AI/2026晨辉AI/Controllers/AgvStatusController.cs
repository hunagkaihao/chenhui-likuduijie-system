using _2026晨辉AI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgvStatusController : ControllerBase
    {
        private readonly RcsApiManager _rcsApiManager;
        private readonly Dictionary<string, string> _mapCodeToFloorMap;

        public AgvStatusController(RcsApiManager rcsApiManager)
        {
            _rcsApiManager = rcsApiManager;
            // 初始化mapCode到楼层的映射
            _mapCodeToFloorMap = new Dictionary<string, string>
            {
                { "AA", "11栋5楼" },
                { "AB", "11栋2楼" },
                { "AC", "婴宝注塑" },
                { "CA", "五栋1楼" },
                { "CB", "五栋2楼" },
                { "CC", "五栋3楼" },
                { "CD", "五栋4楼" },
                { "CE", "五栋5楼" },
                { "CF", "五栋6楼" }
            };
        }

        /// <summary>
        /// 查询AGV状态
        /// </summary>
        /// <param name="mapCode">地图编码</param>
        /// <returns>AGV状态数据</returns>
        [HttpGet("query")]
        public async Task<IActionResult> QueryAgvStatus([FromQuery] string mapCode = "AA")
        {
            try
            {
                string reqCode = "QUERY" + System.DateTime.Now.ToString("yyyyMMddHHmmss");
                // 确保使用与楼层映射一致的mapCode
                var result = await _rcsApiManager.QueryAgvStatusAsync(reqCode, mapCode);
                
                // 为每个AGV添加楼层信息
                if (result.code == "0" && result.data != null)
                {
                    foreach (var agv in result.data)
                    {
                        if (_mapCodeToFloorMap.TryGetValue(agv.mapCode, out string floor))
                        {
                            agv.mapCode = floor; // 替换为楼层名称
                        }
                    }
                }
                
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 查询所有AGV状态
        /// </summary>
        /// <returns>所有AGV状态数据</returns>
        [HttpGet("query-all")]
        public async Task<IActionResult> QueryAllAgvStatus()
        {
            try
            {
                // 定义所有需要查询的mapCode
                string[] mapCodes = { "AA", "AB", "AC", "CA", "CB", "CC", "CD", "CE", "CF" };
                List<ResultAgvStatusListDto> allAgvData = new List<ResultAgvStatusListDto>();
                
                foreach (var mapCode in mapCodes)
                {
                    string reqCode = "QUERY" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + mapCode;
                    var result = await _rcsApiManager.QueryAgvStatusAsync(reqCode, mapCode);
                    
                    if (result.code == "0" && result.data != null)
                    {
                        // 为每个AGV添加楼层信息
                        foreach (var agv in result.data)
                        {
                            if (_mapCodeToFloorMap.TryGetValue(agv.mapCode, out string floor))
                            {
                                agv.mapCode = floor; // 替换为楼层名称
                            }
                            allAgvData.Add(agv);
                        }
                    }
                }
                
                return Ok(new {
                    code = "0",
                    message = "Success",
                    data = allAgvData
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
# 巷道任务页面 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增"巷道任务"页面：绑定物料信息到 cell 表，并在巷道外侧无货时下发 AGV 任务给 RCS。

**Architecture:** 前端新增 `LaneTaskView.vue`（参考 MaterialAndAgvTaskView）+ 路由 + 首页卡片；后端新增 `LaneTasksController`（bind 保存/更新 cell，dispatch 先查同巷道最小 Location 的外侧 cell 状态，无货才调用 RCS 下发并入库 AgvTask）；Cell 模型新增 PalletCode/Picima/Tepi 三列，启动时对已有库 ALTER TABLE 补列。

**Tech Stack:** ASP.NET Core 8 (EF Core + Pomelo MySQL 9.0)、Vue 3 + Vite 4 + vue-router 4 + axios 1.7

## Global Constraints

- 不修改现有 `AgvTasksController.cs`、`MaterialAndAgvTaskView.vue`、`BindMaterialView.vue`、`AddAgvTaskView.vue`（用户指定只做参考）。
- 巷道外侧判定：同 `CellCode`（同巷道）中 `Location` 按字符串升序最小者；`Status=="Occupied"` 即有货。
- 绑定映射：物料→`BindMaterialCode`、托盘→`PalletCode`、批次→`Picima`、检验→`Tepi`、起始位置→`Location`、`Status="Occupied"`。
- AGV 任务：`FromLocation=起始位置`、`ToLocation="LK02"`、TaskType 前缀映射（A→Y01、CP→Y02、其他→Y1105）、RCS 成功才入库且 `Status="InProgress"`。
- 绑定成功后**保留起始位置**；添加任务**下发成功后才清空**（含起始位置）；有货被拦则保留表单供重试。
- 项目无单元测试框架：所有任务用 `dotnet build` / `npm run build` 作为验证门。
- 前端新接口统一走 `/api/LaneTasks/...`（默认 axios，与 CellsView 的 `/api/Cells` 约定一致）。

---

### Task 1: Cell 模型新增列 + 启动时补列

**Files:**
- Modify: `2026晨辉AI/2026晨辉AI/Models/Cell.cs:29-33`
- Modify: `2026晨辉AI/2026晨辉AI/Program.cs:55-79`

**Interfaces:**
- Produces: `Cell` 新增属性 `PalletCode`、`Picima`、`Tepi`（`string?`，均 `[StringLength(200)]`）。Task 2 使用这三个属性。
- Produces: `Program.cs` 启动时对已有 `Cells` 表补齐三列，供既有数据库使用。

- [ ] **Step 1: 修改 Cell.cs，新增三列**

在 `Models/Cell.cs` 的 `BindQuantity`（第 33 行 `public int? BindQuantity { get; set; }`）之后插入：

```csharp
        [StringLength(200)]
        public string? PalletCode { get; set; }
        
        [StringLength(200)]
        public string? Picima { get; set; }
        
        [StringLength(200)]
        public string? Tepi { get; set; }
```

- [ ] **Step 2: 修改 Program.cs，启动时补列**

在 `Program.cs` 中 `using (var scope = app.Services.CreateScope())` 块内、`dbContext.Database.EnsureCreated();` 与新建 BindRecords 表的 `try` 块之间，插入以下 `try` 块（使用既有 `connectionString` 变量）：

```csharp
    // 手动补充 Cells 表新增列（EnsureCreated 不会在已存在的表上新增列）
    try
    {
        using (var connection = new MySqlConnector.MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var existingColumns = new HashSet<string>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Cells'";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        existingColumns.Add(reader.GetString(0));
                    }
                }
            }
            foreach (var column in new[] {
                ("PalletCode", "varchar(200) NULL"),
                ("Picima", "varchar(200) NULL"),
                ("Tepi", "varchar(200) NULL")
            })
            {
                if (!existingColumns.Contains(column.Item1))
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = $"ALTER TABLE Cells ADD COLUMN {column.Item1} {column.Item2}";
                        await cmd.ExecuteNonQueryAsync();
                    }
                    Console.WriteLine($"Cells 表新增列 {column.Item1} 完成");
                }
            }
        }
        Console.WriteLine("Cells 表结构已就绪");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"补充 Cells 表列失败: {ex.Message}");
    }
```

- [ ] **Step 3: 编译验证**

Run（workdir=`2026晨辉AI/2026晨辉AI`）：`dotnet build`
Expected: `Build succeeded`，0 个错误。

- [ ] **Step 4: Commit**

```bash
git add "2026晨辉AI/2026晨辉AI/Models/Cell.cs" "2026晨辉AI/2026晨辉AI/Program.cs"
git commit -m "feat: Cell表新增托盘/批次/检验列及启动时补列"
```

---

### Task 2: LaneTasksController（bind + dispatch）

**Files:**
- Create: `2026晨辉AI/2026晨辉AI/Controllers/LaneTasksController.cs`

**Interfaces:**
- Consumes: `Cell.PalletCode/Picima/Tepi`（Task 1）、`RcsApiManager.CreateAgvTaskAsync(reqCode, taskTyp, ctnrTyp, userCallCodePath, taskCode, boxCode, agvCode)`（已存在，返回 `ResultAgvTaskDto`）。
- Produces: `POST /api/LaneTasks/bind`、`POST /api/LaneTasks/dispatch`。Task 3 调用这两个接口。

- [ ] **Step 1: 创建控制器文件**

创建 `Controllers/LaneTasksController.cs`，内容如下：

```csharp
using _2026晨辉AI.Data;
using _2026晨辉AI.Models;
using _2026晨辉AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LaneTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RcsApiManager _rcsApiManager;
        private readonly ILogger<LaneTasksController> _logger;

        public LaneTasksController(ApplicationDbContext context, RcsApiManager rcsApiManager, ILogger<LaneTasksController> logger)
        {
            _context = context;
            _rcsApiManager = rcsApiManager;
            _logger = logger;
        }

        // POST: api/LaneTasks/bind
        [HttpPost("bind")]
        public async Task<ActionResult<object>> Bind([FromBody] LaneBindDto input)
        {
            if (string.IsNullOrWhiteSpace(input.FromLocation) ||
                string.IsNullOrWhiteSpace(input.MaterialId) ||
                string.IsNullOrWhiteSpace(input.PalletId))
            {
                return BadRequest(new { success = false, message = "物料条码、托盘条码、起始位置为必填项" });
            }

            var cell = await _context.Cells.FirstOrDefaultAsync(c => c.Location == input.FromLocation);
            if (cell == null)
            {
                cell = new Cell
                {
                    CellCode = input.FromLocation,
                    CellType = "巷道",
                    Location = input.FromLocation,
                    Status = "Occupied",
                    CreatedAt = DateTime.Now
                };
                _context.Cells.Add(cell);
            }

            cell.BindMaterialCode = input.MaterialId;
            cell.PalletCode = input.PalletId;
            cell.Picima = input.Picima;
            cell.Tepi = input.Tepi;
            cell.Status = "Occupied";
            cell.LastUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("巷道任务绑定成功: 起始位置={FromLocation}, 物料={MaterialId}, 托盘={PalletId}",
                input.FromLocation, input.MaterialId, input.PalletId);
            return new { success = true, message = "绑定成功" };
        }

        // POST: api/LaneTasks/dispatch
        [HttpPost("dispatch")]
        public async Task<ActionResult<object>> Dispatch([FromBody] LaneDispatchDto input)
        {
            if (string.IsNullOrWhiteSpace(input.FromLocation))
            {
                return BadRequest(new { success = false, message = "起始位置为必填项" });
            }

            var cell = await _context.Cells.FirstOrDefaultAsync(c => c.Location == input.FromLocation);
            if (cell == null)
            {
                return new { success = false, message = "起始位置不存在，请先绑定物料" };
            }

            // 查找巷道外侧：同 CellCode 中 Location 最小者
            var laneCells = await _context.Cells
                .Where(c => c.CellCode == cell.CellCode)
                .OrderBy(c => c.Location)
                .ToListAsync();
            var outerCell = laneCells.FirstOrDefault();
            if (outerCell != null && outerCell.Status == "Occupied")
            {
                _logger.LogInformation("巷道外侧有货物，暂不下发: 巷道={Lane}, 外侧点位={Location}",
                    cell.CellCode, outerCell.Location);
                return new { success = false, blocked = true, message = "巷道外侧有货物，请等待外侧货物清空后再下发" };
            }

            // 同一起点不允许存在多个未完成任务
            var activeStatuses = new[] { "Pending", "InProgress", "Waiting" };
            var existing = await _context.AgvTasks
                .FirstOrDefaultAsync(t => t.FromLocation == input.FromLocation && activeStatuses.Contains(t.Status));
            if (existing != null)
            {
                return new { success = false, message = $"起点 {input.FromLocation} 已存在未完成任务（任务编码：{existing.TaskCode}），请等待完成后再下发" };
            }

            var taskCode = "AGV" + DateTime.Now.ToString("yyyyMMddHHmmss");
            string taskTyp = "Y1105";
            if (input.FromLocation.StartsWith("A"))
            {
                taskTyp = "Y01";
            }
            if (input.FromLocation.StartsWith("CP"))
            {
                taskTyp = "Y02";
            }

            try
            {
                string reqCode = "REQ" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string ctnrTyp = "1";
                string[] userCallCodePath = new string[] { input.FromLocation, "LK02" };
                string boxCode = "BOX" + DateTime.Now.ToString("yyyyMMddHHmmss");

                var result = await _rcsApiManager.CreateAgvTaskAsync(reqCode, taskTyp, ctnrTyp, userCallCodePath, taskCode, boxCode, null);

                if (result.code == "0")
                {
                    var agvTask = new AgvTask
                    {
                        TaskCode = taskCode,
                        TaskType = taskTyp,
                        Status = "InProgress",
                        FromLocation = input.FromLocation,
                        ToLocation = "LK02",
                        StartedAt = DateTime.Now,
                        CreatedAt = DateTime.Now
                    };
                    _context.AgvTasks.Add(agvTask);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("巷道任务下发成功: TaskCode={TaskCode}, 路径={From}->LK02",
                        taskCode, input.FromLocation);
                    return new { success = true, message = result.message, taskCode = result.taskCode };
                }
                else
                {
                    _logger.LogWarning("巷道任务下发失败: TaskCode={TaskCode}, 原因={Message}", taskCode, result.message);
                    return new { success = false, message = result.message };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "巷道任务下发异常: TaskCode={TaskCode}", taskCode);
                return new { success = false, message = ex.Message };
            }
        }
    }

    public class LaneBindDto
    {
        public string FromLocation { get; set; }
        public string MaterialId { get; set; }
        public string PalletId { get; set; }
        public string? Picima { get; set; }
        public string? Tepi { get; set; }
    }

    public class LaneDispatchDto
    {
        public string FromLocation { get; set; }
    }
}
```

- [ ] **Step 2: 编译验证**

Run（workdir=`2026晨辉AI/2026晨辉AI`）：`dotnet build`
Expected: `Build succeeded`，0 个错误。注意 `ResultAgvTaskDto` 两个重名类（`_2026晨辉AI.Services` 与 `_2026晨辉AI.Controllers`）——本文件只引用 `RcsApiManager`，其返回类型解析为 Services 命名空间版本，无需额外 using。

- [ ] **Step 3: Commit**

```bash
git add "2026晨辉AI/2026晨辉AI/Controllers/LaneTasksController.cs"
git commit -m "feat: 新增巷道任务控制器（绑定cell与巷道外侧检查后下发RCS）"
```

---

### Task 3: 前端 LaneTaskView.vue

**Files:**
- Create: `2026晨辉AI/frontend/src/views/LaneTaskView.vue`

**Interfaces:**
- Consumes: `POST /api/LaneTasks/bind`（body: fromLocation/materialId/palletId/picima/tepi）、`POST /api/LaneTasks/dispatch`（body: fromLocation）、`GET /pda-api/PDA/RK/getBSMData`（扫码查物料，params: biaoshima/company）。

- [ ] **Step 1: 创建页面组件**

创建 `frontend/src/views/LaneTaskView.vue`，内容如下（界面参考 MaterialAndAgvTaskView，不改动该参考文件）：

```vue
<template>
  <div class="lane-task">
    <div class="header">
      <h2>巷道任务</h2>
      <button class="btn-back" @click="goHome">
        <span class="back-icon">←</span> 返回
      </button>
    </div>

    <div class="section">
      <div class="combined-form">
        <div class="form-group form-group-inline">
          <label for="materialId">物料条码:</label>
          <div class="input-group">
            <input type="text" id="materialId" ref="materialInput" v-model="materialForm.materialId" required placeholder="请扫描物料条码" @keydown.enter.prevent="scanMaterial()" @input="handleMaterialInput">
            <button type="button" class="btn-clear" @click="clearMaterialId">×</button>
          </div>
        </div>
        <div v-if="materialError" class="error-message">
          {{ materialError }}
        </div>

        <div class="form-group form-group-inline">
          <label for="palletId">托盘条码:</label>
          <div class="input-group">
            <input type="text" id="palletId" ref="palletInput" v-model="materialForm.palletId" required placeholder="请输入或扫描托盘条码" @input="handlePalletInput">
            <button type="button" class="btn-clear" @click="clearPalletId">×</button>
          </div>
        </div>
        <div class="form-group form-group-inline">
          <label for="picima">批次码:</label>
          <div class="input-group">
            <input type="text" id="picima" v-model="materialForm.picima" placeholder="请输入批次码">
            <button type="button" class="btn-clear" @click="materialForm.picima = ''">×</button>
          </div>
        </div>
        <div class="form-group form-group-inline">
          <label for="tepi">检验码:</label>
          <div class="input-group">
            <input type="text" id="tepi" v-model="materialForm.tepi" placeholder="请输入检验码">
            <button type="button" class="btn-clear" @click="materialForm.tepi = ''">×</button>
          </div>
        </div>
        <div class="form-group form-group-inline">
          <label for="fromLocation">起始位置:</label>
          <div class="input-group">
            <input type="text" id="fromLocation" ref="fromLocationInput" v-model="agvTask.fromLocation" required placeholder="例如：A00099">
            <button type="button" class="btn-clear" @click="clearFromLocation">×</button>
          </div>
        </div>

        <div v-if="Object.keys(materialInfo).length > 0" class="material-info">
          <h4>物料信息</h4>
          <div class="info-item">
            <span>名称: </span><span>{{ materialInfo.name || '未知' }}</span>
          </div>
          <div class="info-item">
            <span>规格: </span><span>{{ materialInfo.spec || '未知' }}</span>
          </div>
          <div class="info-item">
            <span>单位: </span><span>{{ materialInfo.unit || '未知' }}</span>
          </div>
        </div>

        <div v-if="bindSuccess" class="success-message">
          {{ bindSuccess }}
        </div>
        <div v-if="taskSuccess" class="success-message">
          {{ taskSuccess }}
        </div>
        <div v-if="taskError" class="error-message">
          {{ taskError }}
        </div>
      </div>
    </div>

    <!-- 按钮容器 -->
    <div class="buttons-container">
      <form @submit.prevent="clearAllData" class="button-form">
        <button type="submit" class="btn btn-danger">清空数据</button>
      </form>
      <form @submit.prevent="bindMaterial" class="button-form">
        <button type="submit" class="btn btn-success" :disabled="bindLock">
          {{ bindLock ? '提交中...' : '绑定物料' }}
        </button>
      </form>
      <form @submit.prevent="addTask" class="button-form">
        <button type="submit" class="btn btn-primary" :disabled="taskLock">
          {{ taskLock ? '提交中...' : '添加任务' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script>
import axios from 'axios'

// 创建独立的 axios 实例，用于 PDA 物料信息查询
const pdaAxios = axios.create({
  baseURL: '' // 空字符串，避免添加任何前缀
})

export default {
  name: 'LaneTaskView',
  data() {
    return {
      materialForm: {
        palletId: '',
        materialId: '',
        picima: '',
        tepi: '',
        quantity: 1
      },
      materialInfo: {},
      materialError: '',
      bindSuccess: '',
      agvTask: {
        fromLocation: ''
      },
      taskSuccess: '',
      taskError: '',
      lastRequestTime: 0,
      bindLock: false,
      taskLock: false
    }
  },
  mounted() {
    this.$nextTick(() => {
      setTimeout(() => {
        if (this.$refs.materialInput) {
          this.$refs.materialInput.focus({ preventScroll: true })
        }
      }, 100)
    })
  },
  methods: {
    goHome() {
      this.$router.push('/')
    },
    clearMaterialId() {
      this.materialForm.materialId = ''
      this.materialInfo = {}
      this.materialError = ''
      this.focusInput('materialInput')
    },
    clearPalletId() {
      this.materialForm.palletId = ''
      this.focusInput('palletInput')
    },
    clearFromLocation() {
      this.agvTask.fromLocation = ''
      this.focusInput('fromLocationInput')
    },
    focusInput(refName) {
      this.$nextTick(() => {
        setTimeout(() => {
          if (this.$refs[refName]) {
            this.$refs[refName].focus({ preventScroll: true })
          }
        }, 100)
      })
    },
    handleMaterialInput() {
      if (this.materialInputTimer) {
        clearTimeout(this.materialInputTimer)
      }
      this.materialInputTimer = setTimeout(() => {
        if (this.materialForm.materialId && this.materialForm.materialId.length > 0) {
          this.scanMaterial()
        }
      }, 400)
    },
    handlePalletInput() {
      if (this.palletInputTimer) {
        clearTimeout(this.palletInputTimer)
      }
      this.palletInputTimer = setTimeout(() => {
        if (this.materialForm.palletId && this.materialForm.palletId.length > 0) {
          this.focusInput('fromLocationInput')
        }
      }, 400)
    },
    async scanMaterial() {
      const currentTime = Date.now()
      if (currentTime - this.lastRequestTime < 1000) return

      if (!this.materialForm.materialId) {
        this.materialError = '请输入物料条码'
        return
      }

      this.materialInfo = {}
      this.materialError = ''

      let company = ''
      const materialId = this.materialForm.materialId
      if (materialId.startsWith('R')) {
        company = '光宝'
      } else if (materialId.startsWith('@RQ')) {
        company = '婴宝'
      }

      try {
        this.lastRequestTime = currentTime
        const response = await pdaAxios.get('/pda-api/PDA/RK/getBSMData', {
          params: {
            biaoshima: this.materialForm.materialId,
            company: company
          }
        })

        if (response.data) {
          const result = response.data
          if (result[0] === 'Y') {
            this.materialError = ''
            this.materialInfo = {
              name: result[1] || '未知',
              spec: result[2] || '未知',
              unit: result[3] || '未知'
            }
            this.materialForm.tepi = '0'
            this.focusInput('palletInput')
          } else {
            const msg = (result[1] || '') + (result[2] || '')
            this.materialError = msg || '获取物料信息失败'
            this.materialInfo = {}
            this.materialForm.materialId = ''
          }
        } else {
          this.materialError = '获取物料信息失败：未知错误'
          this.materialInfo = {}
          this.materialForm.materialId = ''
        }
      } catch (error) {
        console.error('扫描物料条码失败:', error)
        this.materialError = '扫描物料条码失败，请检查网络连接或API地址'
        this.materialInfo = {}
        this.materialForm.materialId = ''
      }
    },
    async bindMaterial() {
      if (this.bindLock) return
      this.bindLock = true
      setTimeout(() => { this.bindLock = false }, 3000)
      try {
        this.materialError = ''
        this.bindSuccess = ''

        if (!this.materialForm.materialId) {
          this.materialError = '请先扫描物料条码'
          return
        }
        if (!this.materialForm.palletId) {
          this.materialError = '请先扫描托盘条码'
          return
        }
        if (!this.agvTask.fromLocation) {
          this.materialError = '请填写起始位置'
          return
        }

        const response = await axios.post('/api/LaneTasks/bind', {
          fromLocation: this.agvTask.fromLocation,
          materialId: this.materialForm.materialId,
          palletId: this.materialForm.palletId,
          picima: this.materialForm.picima,
          tepi: this.materialForm.tepi
        })

        if (response.data && response.data.success) {
          this.bindSuccess = '绑定成功！'
          // 绑定成功：清空物料/托盘/批次/检验码，保留起始位置
          this.materialForm = {
            palletId: '',
            materialId: '',
            picima: '',
            tepi: '',
            quantity: 1
          }
          this.materialInfo = {}
          setTimeout(() => { this.bindSuccess = '' }, 3000)
          this.focusInput('materialInput')
        } else {
          this.materialError = '绑定失败: ' + (response.data?.message || '未知错误')
        }
      } catch (error) {
        console.error('绑定物料失败:', error)
        this.materialError = '绑定物料失败: ' + (error.response?.data?.message || error.message)
      }
    },
    async addTask() {
      if (this.taskLock) return
      this.taskLock = true
      setTimeout(() => { this.taskLock = false }, 3000)
      try {
        this.taskSuccess = ''
        this.taskError = ''

        if (!this.agvTask.fromLocation) {
          this.taskError = '请填写起始位置'
          return
        }

        const response = await axios.post('/api/LaneTasks/dispatch', {
          fromLocation: this.agvTask.fromLocation
        })

        if (response.data && response.data.success) {
          this.taskSuccess = '任务下发成功！'
          // 下发成功：清空全部（含起始位置）
          await this.resetAll()
          setTimeout(() => { this.taskSuccess = '' }, 3000)
          this.focusInput('materialInput')
        } else if (response.data && response.data.blocked) {
          // 巷道外侧有货：保留表单，等待后可重试
          this.taskError = response.data.message || '巷道外侧有货物，请等待外侧货物清空后再下发'
        } else {
          this.taskError = '任务下发失败: ' + (response.data?.message || '未知错误')
        }
      } catch (error) {
        console.error('添加任务失败:', error)
        this.taskError = '添加任务失败: ' + (error.response?.data?.message || error.message)
      }
    },
    async resetAll() {
      this.materialForm = {
        palletId: '',
        materialId: '',
        picima: '',
        tepi: '',
        quantity: 1
      }
      this.materialInfo = {}
      this.materialError = ''
      this.agvTask = { fromLocation: '' }
      this.taskSuccess = ''
      this.taskError = ''
    },
    clearAllData() {
      this.resetAll()
      this.focusInput('materialInput')
    }
  }
}
</script>

<style scoped>
.lane-task {
  margin-top: 10px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.btn-back {
  background-color: #007bff;
  color: white;
  padding: 6px 12px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.btn-back:hover {
  background-color: #0069d9;
}

.back-icon {
  font-size: 14px;
  font-weight: bold;
}

.section {
  margin-bottom: 5px;
}

.error-message {
  background-color: #fee;
  color: #c00;
  padding: 8px;
  border-radius: 4px;
  margin-bottom: 10px;
  border: 1px solid #fcc;
  font-size: 12px;
}

.success-message {
  background-color: #e8f5e8;
  color: #2e7d32;
  padding: 8px;
  border-radius: 4px;
  margin-bottom: 12px;
  border: 1px solid #c8e6c9;
  font-size: 14px;
}

.input-group {
  position: relative;
  display: flex;
  align-items: center;
}

.input-group input {
  flex: 1;
  padding-right: 25px;
}

.btn-clear {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  font-size: 16px;
  color: #999;
  cursor: pointer;
  padding: 0;
  width: 18px;
  height: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
}

.btn-clear:hover {
  background-color: #f0f0f0;
  color: #666;
}

.combined-form {
  background-color: #f9f9f9;
  padding: 12px;
  border-radius: 6px;
  margin-bottom: 8px;
}

.form-group {
  margin-bottom: 8px;
}

.form-group-inline {
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-group-inline label {
  flex: 0 0 auto;
  margin-bottom: 0;
  font-weight: bold;
  font-size: 14px;
}

.form-group-inline .input-group {
  flex: 1;
}

label {
  display: block;
  margin-bottom: 4px;
  font-weight: bold;
  font-size: 14px;
}

input {
  width: 100%;
  padding: 6px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

.material-info {
  background-color: #e8f5e8;
  padding: 10px;
  border-radius: 4px;
  margin-bottom: 12px;
  border: 1px solid #c8e6c9;
}

.material-info h4 {
  margin: 0 0 8px 0;
  font-size: 14px;
  color: #2e7d32;
}

.info-item {
  margin-bottom: 4px;
  font-size: 13px;
}

.info-item span:first-child {
  font-weight: bold;
  margin-right: 8px;
}

.btn {
  padding: 8px 12px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  width: 100%;
  margin-top: 10px;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-primary:hover {
  background-color: #0069d9;
}

.btn-success {
  background-color: #4CAF50;
  color: white;
}

.btn-success:hover {
  background-color: #45a049;
}

.btn-danger {
  background-color: #f44336;
  color: white;
}

.btn-danger:hover {
  background-color: #da190b;
}

.buttons-container {
  display: flex;
  gap: 8px;
  margin-top: 10px;
}

.button-form {
  flex: 1;
  margin: 0;
}

.button-form .btn {
  width: 100%;
  margin-top: 0;
}
</style>
```

- [ ] **Step 2: 构建验证**

Run（workdir=`2026晨辉AI/frontend`）：`npm run build`
Expected: `✓ built in ...`，无报错。产物写入 `frontend/dist/`。

- [ ] **Step 3: Commit**

```bash
git add "2026晨辉AI/frontend/src/views/LaneTaskView.vue"
git commit -m "feat: 新增巷道任务页面"
```

---

### Task 4: 路由 + 首页入口

**Files:**
- Modify: `2026晨辉AI/frontend/src/router/index.js:1-11`
- Modify: `2026晨辉AI/frontend/src/views/HomeView.vue:22-28`

**Interfaces:**
- Consumes: `LaneTaskView`（Task 3，默认导出组件）。

- [ ] **Step 1: 注册路由**

在 `frontend/src/router/index.js` 的 import 区，`import BindMaterialView from '../views/BindMaterialView.vue'` 之后加一行：

```javascript
import LaneTaskView from '../views/LaneTaskView.vue'
```

在 routes 数组中，`/bind-material` 路由项之后加：

```javascript
  {
    path: '/lane-task',
    name: 'LaneTask',
    component: LaneTaskView
  },
```

- [ ] **Step 2: 首页添加卡片**

在 `frontend/src/views/HomeView.vue` 中 `/bind-material` 卡片项（第 22-28 行）之后插入：

```html
      <router-link to="/lane-task" class="功能卡片">
        <div class="卡片内容">
          <h3>巷道任务</h3>
          <p>绑定物料并在巷道外侧无货时下发AGV任务</p>
        </div>
      </router-link>
```

- [ ] **Step 3: 构建验证**

Run（workdir=`2026晨辉AI/frontend`）：`npm run build`
Expected: `✓ built in ...`，无报错。

- [ ] **Step 4: Commit**

```bash
git add "2026晨辉AI/frontend/src/router/index.js" "2026晨辉AI/frontend/src/views/HomeView.vue"
git commit -m "feat: 注册巷道任务路由与首页入口"
```

---

### Task 5: 整体验证

**Files:** 无代码改动，仅验证。

- [ ] **Step 1: 后端编译**

Run（workdir=`2026晨辉AI/2026晨辉AI`）：`dotnet build`
Expected: `Build succeeded`，0 个错误、0 个警告业务级错误。

- [ ] **Step 2: 前端构建**

Run（workdir=`2026晨辉AI/frontend`）：`npm run build`
Expected: `✓ built in ...`，无报错。

- [ ] **Step 3: 手工验证清单（需现场运行）**

1. 首页出现"巷道任务"卡片，点击进入 `/lane-task`。
2. 扫码物料条码 → 显示物料信息面板，聚焦托盘输入框。
3. 输入托盘条码与起始位置（如 `A00099`），点"绑定物料" → 提示"绑定成功！"；`cell` 表出现/更新 `Location=A00099` 记录，`Status=Occupied`、`BindMaterialCode/PalletCode/Picima/Tepi` 已写入；**起始位置保留**、物料/托盘已清空。
4. 再点"添加任务" → 若该巷道最外侧 cell 为 `Occupied`，提示"巷道外侧有货物…"且**表单保留**；若外侧为 `Available`/无纪录，则 RCS 下发成功，`AgvTasks` 表新增 `InProgress` 记录，**全表单清空**。
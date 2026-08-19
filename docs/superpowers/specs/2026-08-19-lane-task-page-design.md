# 巷道任务页面 设计文档

日期：2026-08-19
项目：chenhui-likuduijie-system（晨辉立库对接系统）

## 背景

在立库对接系统中新增"巷道任务"独立页面。物料到达巷道外侧后，操作员扫描物料条码、托盘条码、批次码、检验码并录入起始位置，先"绑定物料"（将物料信息与起始位置保存到数据库 cell 表），再"添加任务"（检查巷道外侧无货后下发 AGV 任务给 RCS）。

## 需求要点

- 新增独立页面"巷道任务"：新路由 + 首页卡片，界面参考现有"物料绑定和添加AGV任务"页面（MaterialAndAgvTaskView），包含三个按钮：**清空数据 / 绑定物料 / 添加任务**。
- 输入字段：物料条码（必填）、托盘条码（必填）、批次码、检验码、起始位置（必填）。
- 点击"绑定物料"：绑定容器并将物料信息和起始位置保存到数据库 cell 表。绑定后**不清空起始位置**。
- 点击"添加任务"：清空表单；先检查巷道外侧是否有货物，无货则下发任务给 RCS；有货则不下发，等待外侧货物清空后才可继续下发。

## 数据模型（cell 表）

现有 `Cell` 模型（Models/Cell.cs）字段：Id、CellCode、CellType、Status、Location、Zone、ShelfCode、BindMaterialCode、BindQuantity、CreatedAt、LastUpdatedAt、Description。

本次语义约定与新增：

- `CellCode`：巷道（巷道）标识，CellCode 相同表示同一巷道。
- `Location`：点位编码（如 `A00099`），数值由小到大表示外侧到内侧；同一巷道内 Location 最小者即"巷道外侧"。
- `BindMaterialCode`：绑定的物料条码。
- `Status`：`Available` / `Occupied`。绑定后置 `Occupied`；外侧 `Occupied` 表示有货，应阻拦下发。

**新增列**（需在既有数据库上 ALTER TABLE，参考 Program.cs 中 BindRecords 表的做法，启动时检查 `information_schema.columns`，缺失则执行 `ALTER TABLE Cells ADD COLUMN ...`）：

| 列名 | 类型 | 含义 |
|------|------|------|
| PalletCode | varchar(200) NULL | 托盘条码 |
| Picima | varchar(200) NULL | 批次码 |
| Tepi | varchar(200) NULL | 检验码 |

字段映射（绑定操作）：

| 输入 | Cell 列 |
|------|---------|
| 物料条码 | BindMaterialCode |
| 托盘条码 | PalletCode |
| 批次码 | Picima |
| 检验码 | Tepi |
| 起始位置 | Location |
| — | Status = "Occupied" |

## 后端

新增 `Controllers/LaneTasksController.cs`（`[Route("api/[controller]")]`），不修改现有 Controller。

### POST /api/LaneTasks/bind

请求体：

```json
{
  "fromLocation": "A00099",
  "materialId": "R1001",
  "palletId": "P001",
  "picima": "20260801",
  "tepi": "0"
}
```

逻辑：
1. 校验 `fromLocation`、`materialId`、`palletId` 必填（缺失返回 400）。
2. 按 `Location == fromLocation` 查 cell 表。
   - 存在 → 更新该行：BindMaterialCode、PalletCode、Picima、Tepi、Status="Occupied"、LastUpdatedAt。
   - 不存在 → 新建一行：CellCode=fromLocation（兜底）、Location=fromLocation、CellType="巷道"、Status="Occupied" 及物料信息。
3. 返回 `{ success: true, message: "绑定成功" }`。

### POST /api/LaneTasks/dispatch

请求体：

```json
{ "fromLocation": "A00099" }
```

逻辑：
1. 按 `Location == fromLocation` 查 cell，不存在 → 返回 `{ success:false, message:"起始位置不存在，请先绑定物料" }`。
2. 求巷道外侧：同 `CellCode` 的 cell 按 `Location` 升序取第一条（`OrderBy(c => c.Location)` 字符串升序，`A00099` 等 0 填充编码可用）。
3. 外侧 cell 存在且 `Status == "Occupied"` → 返回 `{ success:false, blocked:true, message:"巷道外侧有货物，请等待外侧货物清空后再下发" }`，不下发。
4. 外侧 cell 不存在或非 Occupied（即外侧无货）→ 复用 AgvTasksController 的下发逻辑（复制到本 Controller，避免改动现有代码）：
   - `TaskCode = "AGV" + yyyyMMddHHmmss`
   - `TaskType`：起始位置以 "A" 开头 → "Y01"；以 "CP" 开头 → "Y02"；否则 "Y1105"
   - 同起点存在 Pending/InProgress/Waiting 任务 → 返回 `{ success:false, message:"起点已有未完成任务..." }`
   - `userCallCodePath = [fromLocation, "LK02"]`，`ctnrTyp="1"`，`boxCode="BOX"+时间戳`，`agvCode=null`
   - 调 `RcsApiManager.CreateAgvTaskAsync(...)`
   - RCS `code=="0"` → 新建 AgvTask 记录（Status="InProgress"、StartedAt=now），返回 `{ success:true, message, taskCode }`
   - RCS 失败 → 返回 `{ success:false, message: RCS消息 }`，不入库

## 前端

### 新增 `src/views/LaneTaskView.vue`

界面与交互参考 `MaterialAndAgvTaskView.vue`（不改动该文件）。元素：

- 表单字段：物料条码、托盘条码、批次码、检验码、起始位置（均带清空 × 按钮，物料条码扫码自动查询物料信息）
- 物料信息面板（扫码返回 name/spec/unit 时显示）
- 三个按钮：清空数据（danger）、绑定物料（success）、添加任务（primary）

方法：

- `scanMaterial()`：复用现有 `pdaAxios.get('/pda-api/PDA/RK/getBSMData', { biaoshima, company })` 逻辑，公司名按条码前缀 R→光宝、@RQ→婴宝 推断；成功显示物料信息并聚焦托盘输入框。
- `bindMaterial()`：校验物料/托盘/起始位置必填 → `POST /api/LaneTasks/bind` → 成功后清空物料/托盘/批次/检验码，**保留起始位置**，聚焦物料输入框。
- `addTask()`：校验起始位置必填 → `POST /api/LaneTasks/dispatch` →
  - 成功（success=true）→ 清空全部表单（含起始位置），提示成功
  - 被拦（blocked=true）→ 提示"巷道外侧有货物…"，**保留表单**便于稍后重试
  - 其他失败 → 提示错误，保留表单
- `clearAllData()`：清空全部字段与提示。

### 路由与首页

- `src/router/index.js`：新增 `/lane-task` → LaneTaskView。
- `src/views/HomeView.vue`：在"物料绑定"卡片下方新增"巷道任务"卡片，链接 `/lane-task`。

## 错误处理与边界

- 必填校验缺失 → 前端提示 + 后端 400。
- 起始位置未绑定就点添加任务 → 后端提示"请先绑定物料"。
- 外侧有货 → 前端保留表单，可重试。
- 同起点已有未完成任务 → 阻止重复下发。
- RCS 调用失败 → 返回失败消息，不入库。

## 验证方式

- 后端：`dotnet build` 通过；启动时自动补齐 Cells 表新列。
- 前端：`npm run build` 通过。
- 手工验证流程：首页进入"巷道任务"→ 扫码物料/托盘 → 填起始位置 → 绑定（cell 表出现 Occupied 记录，起始位置保留）→ 添加任务（外侧无货则 RCS 下发成功并清空；外侧有货则提示并保留）。

# 2026晨辉AI 后台服务

基于 .NET 8.0 + MySQL 的后台服务搭建指南

## 环境要求

- .NET 8.0 SDK
- MySQL 5.7 或更高版本

## 配置步骤

1. **安装 MySQL**
   - 下载并安装 MySQL：https://dev.mysql.com/downloads/
   - 启动 MySQL 服务

2. **创建数据库**
   - 登录 MySQL 命令行或 phpMyAdmin
   - 创建名为 `chenghuiecs` 的数据库

3. **配置数据库连接**
   - 打开 `appsettings.json` 文件
   - 修改 `ConnectionStrings` 部分，设置正确的 MySQL 用户名和密码

4. **安装依赖包**
   ```bash
   dotnet restore
   ```

5. **运行应用**
   ```bash
   dotnet run
   ```

## API 接口

### 用户接口
- `GET /api/Users` - 获取所有用户
- `GET /api/Users/{id}` - 获取指定用户
- `POST /api/Users` - 创建新用户
- `PUT /api/Users/{id}` - 更新用户信息
- `DELETE /api/Users/{id}` - 删除用户

### 设备任务接口
- `GET /api/DeviceTasks` - 获取所有任务
- `GET /api/DeviceTasks/{id}` - 获取指定任务
- `POST /api/DeviceTasks` - 下发新任务
- `PUT /api/DeviceTasks/{id}` - 更新任务状态
- `DELETE /api/DeviceTasks/{id}` - 删除任务
- `GET /api/DeviceTasks/device/{deviceId}` - 获取指定设备的任务

## 注意事项

- 首次运行时，系统会自动创建数据库表结构
- 请确保 MySQL 服务正在运行
- 请确保数据库连接字符串配置正确

## 前端项目

### 环境要求

- Node.js 18.0.0 或更高版本
- npm 8.0.0 或更高版本

### 配置步骤

1. **安装依赖**
   ```bash
   cd frontend
   npm install
   ```

2. **运行前端应用**
   ```bash
   npm run dev
   ```

3. **访问前端应用**
   - 默认地址：http://localhost:5173

### 功能说明

- 设备任务管理页面：展示所有设备任务
- 添加上发任务：填写设备ID、任务类型和任务内容
- 更新任务状态：通过下拉菜单更新任务状态
- 删除任务：删除指定任务

### 技术栈

- Vue 3.0
- Vue Router
- Axios
- Vite

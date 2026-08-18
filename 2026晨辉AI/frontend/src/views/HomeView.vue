<template>
  <div class="home">
    <div class="功能卡片容器">
      <!-- <router-link to="/material-scan" class="功能卡片">
        <div class="卡片内容">
          <h3>物料扫描绑定</h3>
          <p>扫描物料条码并绑定到托盘</p>
        </div>
      </router-link>
      <router-link to="/add-agv-task" class="功能卡片">
        <div class="卡片内容">
          <h3>添加AGV任务</h3>
          <p>创建新的AGV任务</p>
        </div>
      </router-link> -->
      <router-link to="/material-agv-task" class="功能卡片">
        <div class="卡片内容">
          <h3>物料绑定与AGV任务</h3>
          <p>在同一页面完成物料绑定和任务创建</p>
        </div>
      </router-link>
      <router-link to="/agv-task-list" class="功能卡片">
        <div class="卡片内容">
          <h3>AGV任务列表</h3>
          <p>查看和管理AGV任务</p>
        </div>
      </router-link>
      <!-- <router-link to="/agv-status" class="功能卡片">
        <div class="卡片内容">
          <h3>AGV状态看板</h3>
          <p>实时查看AGV运行状态</p>
        </div>
      </router-link> -->
      <router-link to="/agv-status-4k" class="功能卡片">
        <div class="卡片内容">
          <h3>AGV状态看板 (4K)</h3>
          <p>4K大屏幕专用版本</p>
        </div>
      </router-link>
    </div>
  </div>
</template>

<script>
export default {
  name: 'HomeView'
}
</script>

<style scoped>
.home {
  margin-top: 30px;
}

.功能卡片容器 {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
  margin-top: 30px;
}

.功能卡片 {
  display: block;
  background-color: #f9f9f9;
  border-radius: 8px;
  padding: 20px;
  text-decoration: none;
  color: #333;
  transition: all 0.3s ease;
  border: 1px solid #ddd;
}

.功能卡片:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1);
  border-color: #4CAF50;
}

.卡片内容 h3 {
  margin-top: 0;
  color: #4CAF50;
  font-size: 16px;
  margin-bottom: 10px;
}

.卡片内容 p {
  margin-bottom: 0;
  color: #666;
  font-size: 14px;
}

/* 非PDA设备的样式 */
:global(.app:not(.app-pda)) .功能卡片容器 {
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 30px;
  margin-top: 40px;
}

:global(.app:not(.app-pda)) .功能卡片 {
  padding: 30px;
}

:global(.app:not(.app-pda)) .卡片内容 h3 {
  font-size: 20px;
  margin-bottom: 15px;
}

:global(.app:not(.app-pda)) .卡片内容 p {
  font-size: 16px;
}
</style>

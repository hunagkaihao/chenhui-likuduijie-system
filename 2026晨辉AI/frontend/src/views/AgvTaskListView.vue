<template>
  <div class="agv-task-list">
    <div class="header">
      <h2>AGV任务列表</h2>
      <button class="btn-back" @click="goHome">
        <span class="back-icon">←</span> 返回
      </button>
    </div>
    
    <!-- AGV任务列表 -->
    <div class="task-list">
      <button class="btn" @click="fetchTasks">刷新任务列表</button>
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>取消任务</th>
              <th>起点</th>
              <th>状态</th>
              <th>任务编码</th>
              <th>任务类型</th>
              <th>目标位置</th>
              <th>AGV编号</th>
              <th>创建时间</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="task in tasks" :key="task.id">
              <td>
                <button v-if="task.status !== 'Completed' && task.status !== 'Canceled'" class="btn btn-warning" @click="cancelTask(task.id)">取消</button>
              </td>
              <td>{{ task.fromLocation || '-' }}</td>
              <td>{{ {Pending:'待处理',InProgress:'执行中',Waiting:'等待中',Cached:'已缓存',Completed:'已完成',Canceled:'已取消'}[task.status] || task.status }}</td>
              <td>{{ task.taskCode }}</td>
              <td>{{ task.taskType }}</td>
              <td>{{ task.toLocation || '-' }}</td>
              <td>{{ task.agvCode || '-' }}</td>
              <td>{{ formatDate(task.createdAt) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="tasks.length === 0" class="empty-message">
        暂无任务
      </div>
    </div>

    <!-- 取消确认弹窗 -->
    <div v-if="showCancelModal" class="modal-overlay" @click.self="showCancelModal = false">
      <div class="modal-dialog">
        <div class="modal-header">取消任务</div>
        <div class="modal-body">确定要取消该任务吗？</div>
        <div class="modal-footer">
          <button class="btn btn-cancel" @click="showCancelModal = false">再想想</button>
          <button class="btn btn-confirm" @click="confirmCancel">确定取消</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'AgvTaskListView',
  data() {
    return {
      tasks: [],
      showCancelModal: false,
      pendingCancelId: null
    }
  },
  mounted() {
    this.fetchTasks()
  },
  methods: {
    goHome() {
      this.$router.push('/')
    },
    async fetchTasks() {
      try {
        console.log('开始获取任务列表...')
        const response = await axios.get('/AgvTasks')
        console.log('获取任务列表成功:', response.data)
        console.log('任务数量:', response.data.length)
        this.tasks = response.data.filter(task => task.status !== 'Completed').sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
      } catch (error) {
        console.error('获取任务列表失败:', error)
        alert('获取任务列表失败: ' + error.message)
      }
    },
    async startTask(id) {
      try {
        await axios.post(`/api/AgvTasks/setAsExecuting/${id}`)
        const task = this.tasks.find(t => t.id === id)
        task.status = 'InProgress'
        task.startedAt = new Date().toISOString()
      } catch (error) {
        console.error('开始任务失败:', error)
      }
    },
    async completeTask(id) {
      try {
        await axios.post(`/api/AgvTasks/setAsCompleted/${id}`)
        const task = this.tasks.find(t => t.id === id)
        task.status = 'Completed'
        task.completedAt = new Date().toISOString()
      } catch (error) {
        console.error('完成任务失败:', error)
      }
    },
    cancelTask(id) {
      this.pendingCancelId = id
      this.showCancelModal = true
    },
    async confirmCancel() {
      const id = this.pendingCancelId
      this.showCancelModal = false
      this.pendingCancelId = null
      try {
        await axios.post(`/AgvTasks/setAsCancel/${id}`)// 原来是 /api/AgvTasks/setAsCancel/${id}
        const task = this.tasks.find(t => t.id === id)
        task.status = 'Canceled'
      } catch (error) {
        console.error('取消任务失败:', error)
        alert('取消任务失败: ' + (error.response?.data?.message || error.message))
      }
    },
    async sendToRcs(id) {
      try {
        const response = await axios.post(`/api/AgvTasks/sendToRcs/${id}`)
        if (response.data.success) {
          alert('任务下发成功！')
          this.fetchTasks()
        } else {
          alert('任务下发失败: ' + response.data.message)
        }
      } catch (error) {
        console.error('下发任务到RCS失败:', error)
        alert('下发任务到RCS失败')
      }
    },
    formatDate(dateString) {
      if (!dateString) return ''
      const date = new Date(dateString)
      return date.toLocaleString('zh-CN')
    }
  }
}
</script>

<style scoped>
.agv-task-list {
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

.task-list {
  margin-top: 15px;
}

.task-list h3 {
  font-size: 14px;
  margin: 0 0 10px 0;
}

.btn {
  background-color: #4CAF50;
  color: white;
  padding: 6px 10px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
  margin-right: 4px;
  margin-bottom: 4px;
  display: inline-block;
}

.btn:hover {
  background-color: #45a049;
}

.btn-danger {
  background-color: #f44336;
}

.btn-danger:hover {
  background-color: #da190b;
}

.btn-primary {
  background-color: #007bff;
}

.btn-primary:hover {
  background-color: #0069d9;
}

.btn-warning {
  background-color: #ff9800;
}

.btn-warning:hover {
  background-color: #e68900;
}

.task-list p {
  font-size: 12px;
  margin: 8px 0;
}

.table-container {
  overflow-x: auto;
  margin-top: 10px;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

th, td {
  padding: 6px 8px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

th {
  background-color: #f2f2f2;
  font-weight: bold;
  white-space: nowrap;
}

th:nth-child(3), td:nth-child(3) {
  min-width: 40px;
}

tr:hover {
  background-color: #f5f5f5;
}

.empty-message {
  text-align: center;
  padding: 20px;
  color: #999;
  font-style: italic;
  font-size: 12px;
}

select {
  font-size: 12px;
  padding: 4px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

/* 弹窗样式 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-dialog {
  background: #fff;
  border-radius: 8px;
  min-width: 300px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.2);
  overflow: hidden;
}

.modal-header {
  padding: 14px 20px 0;
  font-size: 16px;
  font-weight: bold;
  color: #333;
}

.modal-body {
  padding: 16px 20px;
  font-size: 14px;
  color: #555;
}

.modal-footer {
  padding: 0 20px 14px;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.btn-cancel {
  background-color: #e0e0e0;
  color: #333;
}

.btn-cancel:hover {
  background-color: #d0d0d0;
}

.btn-confirm {
  background-color: #ff9800;
  color: white;
}

.btn-confirm:hover {
  background-color: #e68900;
}
</style>
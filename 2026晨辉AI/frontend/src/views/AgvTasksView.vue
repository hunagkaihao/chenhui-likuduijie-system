<template>
  <div class="agv-tasks">
    <div class="header">
      <h2>AGV任务管理</h2>
      <button class="btn-back" @click="goHome">
        <span class="back-icon">←</span> 返回主页
      </button>
    </div>
    
    <!-- 添加AGV任务表单 -->
    <div class="task-form">
      <h3>添加AGV任务</h3>
      <form @submit.prevent="addTask">
        <div class="form-group">
          <label for="fromLocation">起始位置:</label>
          <input type="text" id="fromLocation" v-model="newTask.fromLocation" required placeholder="例如：A1-01-01">
        </div>
        <div class="form-group">
          <label for="toLocation">目标位置:</label>
          <input type="text" id="toLocation" v-model="newTask.toLocation" required placeholder="例如：B1-01-01">
        </div>
        <button type="submit" class="btn">添加任务</button>
      </form>
    </div>
    
    <!-- AGV任务列表 -->
    <div class="task-list">
      <h3>任务列表</h3>
      <button class="btn" @click="fetchTasks">刷新任务列表</button>
      <p>当前任务数量: {{ tasks.length }}</p>
      <table>
        <thead>
          <tr>
            <th>任务编码</th>
            <th>任务类型</th>
            <th>状态</th>
            <th>起始位置</th>
            <th>目标位置</th>
            <th>AGV编号</th>
            <th>创建时间</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="task in tasks" :key="task.id">
            <td>{{ task.taskCode }}</td>
            <td>{{ task.taskType }}</td>
            <td>
              <select v-model="task.status" @change="updateTaskStatus(task.id, task.status)">
                <option value="Pending">待处理</option>
                <option value="InProgress">执行中</option>
                <option value="Completed">已完成</option>
                <option value="Canceled">已取消</option>
              </select>
            </td>
            <td>{{ task.fromLocation || '-' }}</td>
            <td>{{ task.toLocation || '-' }}</td>
            <td>{{ task.agvCode || '-' }}</td>
            <td>{{ formatDate(task.createdAt) }}</td>
            <td>
              <button v-if="task.status === 'Pending'" class="btn" @click="startTask(task.id)">开始</button>
              <button v-if="task.status === 'InProgress'" class="btn" @click="completeTask(task.id)">完成</button>
              <button v-if="task.status === 'Pending'" class="btn btn-primary" @click="sendToRcs(task.id)">下发到RCS</button>
              <button class="btn btn-danger" @click="deleteTask(task.id)">删除</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="tasks.length === 0" class="empty-message">
        暂无任务
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'AgvTasksView',
  data() {
    return {
      tasks: [],
      newTask: {
        fromLocation: '',
        toLocation: ''
      }
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
        this.tasks = response.data
      } catch (error) {
        console.error('获取任务列表失败:', error)
        alert('获取任务列表失败: ' + error.message)
      }
    },
    async addTask() {
      try {
        // 验证表单数据
        if (!this.newTask.fromLocation || !this.newTask.toLocation) {
          alert('请填写起始位置和目标位置')
          return
        }
        
        // 准备任务数据
        const taskData = {
          fromLocation: this.newTask.fromLocation,
          toLocation: this.newTask.toLocation,
          status: 'Pending'
        }
        
        console.log('开始添加任务:', taskData)
        const response = await axios.post('/AgvTasks', taskData)
        console.log('添加任务成功:', response.data)
        this.tasks.push(response.data)
        // 重置表单
        this.newTask = {
          fromLocation: '',
          toLocation: ''
        }
      } catch (error) {
        console.error('添加任务失败:', error)
        alert('添加任务失败: ' + error.message)
      }
    },
    async updateTaskStatus(id, status) {
      try {
        const task = this.tasks.find(t => t.id === id)
        await axios.put(`/AgvTasks/${id}`, {
          ...task,
          status
        })
      } catch (error) {
        console.error('更新任务状态失败:', error)
      }
    },
    async startTask(id) {
      try {
        await axios.post(`/AgvTasks/setAsExecuting/${id}`)
        const task = this.tasks.find(t => t.id === id)
        task.status = 'InProgress'
        task.startedAt = new Date().toISOString()
      } catch (error) {
        console.error('开始任务失败:', error)
      }
    },
    async completeTask(id) {
      try {
        await axios.post(`/AgvTasks/setAsCompleted/${id}`)
        const task = this.tasks.find(t => t.id === id)
        task.status = 'Completed'
        task.completedAt = new Date().toISOString()
      } catch (error) {
        console.error('完成任务失败:', error)
      }
    },
    async deleteTask(id) {
      try {
        await axios.delete(`/AgvTasks/${id}`)
        this.tasks = this.tasks.filter(task => task.id !== id)
      } catch (error) {
        console.error('删除任务失败:', error)
      }
    },
    async sendToRcs(id) {
      try {
        const response = await axios.post(`/AgvTasks/sendToRcs/${id}`)
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
.agv-tasks {
  margin-top: 20px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.btn-back {
  background-color: #007bff;
  color: white;
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 5px;
}

.btn-back:hover {
  background-color: #0069d9;
}

.back-icon {
  font-size: 16px;
  font-weight: bold;
}

.task-form {
  background-color: #f9f9f9;
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 30px;
}

.form-group {
  margin-bottom: 15px;
}

label {
  display: block;
  margin-bottom: 5px;
  font-weight: bold;
}

input, textarea {
  width: 100%;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

textarea {
  height: 100px;
  resize: vertical;
}

.btn {
  background-color: #4CAF50;
  color: white;
  padding: 10px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  margin-right: 5px;
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

.task-list {
  margin-top: 30px;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

th {
  background-color: #f2f2f2;
  font-weight: bold;
}

tr:hover {
  background-color: #f5f5f5;
}

.empty-message {
  text-align: center;
  padding: 40px;
  color: #999;
  font-style: italic;
}
</style>

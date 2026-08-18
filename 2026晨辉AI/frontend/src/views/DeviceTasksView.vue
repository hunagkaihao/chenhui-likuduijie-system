<template>
  <div class="device-tasks">
    <h2>设备任务管理</h2>
    
    <!-- 添加上发任务表单 -->
    <div class="task-form">
      <h3>添加上发任务</h3>
      <form @submit.prevent="addTask">
        <div class="form-group">
          <label for="deviceId">设备ID:</label>
          <input type="text" id="deviceId" v-model="newTask.deviceId" required>
        </div>
        <div class="form-group">
          <label for="taskType">任务类型:</label>
          <input type="text" id="taskType" v-model="newTask.taskType" required>
        </div>
        <div class="form-group">
          <label for="taskContent">任务内容:</label>
          <textarea id="taskContent" v-model="newTask.taskContent" required></textarea>
        </div>
        <button type="submit" class="btn">添加上发任务</button>
      </form>
    </div>
    
    <!-- 任务列表 -->
    <div class="task-list">
      <h3>任务列表</h3>
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>设备ID</th>
            <th>任务类型</th>
            <th>任务内容</th>
            <th>状态</th>
            <th>创建时间</th>
            <th>完成时间</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="task in tasks" :key="task.id">
            <td>{{ task.id }}</td>
            <td>{{ task.deviceId }}</td>
            <td>{{ task.taskType }}</td>
            <td>{{ task.taskContent }}</td>
            <td>
              <select v-model="task.status" @change="updateTaskStatus(task.id, task.status)">
                <option value="Pending">待处理</option>
                <option value="InProgress">处理中</option>
                <option value="Completed">已完成</option>
                <option value="Failed">失败</option>
              </select>
            </td>
            <td>{{ formatDate(task.createdAt) }}</td>
            <td>{{ task.completedAt ? formatDate(task.completedAt) : '-' }}</td>
            <td>
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
// 设备任务管理组件
// 功能：
// 1. 添加上发任务
// 2. 查看任务列表
// 3. 更新任务状态
// 4. 删除任务
import axios from 'axios'

export default {
  name: 'DeviceTasksView',
  data() {
    return {
      tasks: [], // 任务列表数据
      newTask: { // 新任务表单数据
        deviceId: '', // 设备ID
        taskType: '', // 任务类型
        taskContent: '' // 任务内容
      }
    }
  },
  mounted() {
    // 组件挂载后获取任务列表
    this.fetchTasks()
  },
  methods: {
    // 获取任务列表
    async fetchTasks() {
      try {
        const response = await axios.get('/DeviceTasks')
        this.tasks = response.data
      } catch (error) {
        console.error('获取任务列表失败:', error)
      }
    },
    // 添加上发任务
    async addTask() {
      try {
        const response = await axios.post('/DeviceTasks', this.newTask)
        this.tasks.push(response.data)
        // 重置表单
        this.newTask = {
          deviceId: '',
          taskType: '',
          taskContent: ''
        }
      } catch (error) {
        console.error('添加任务失败:', error)
      }
    },
    // 更新任务状态
    async updateTaskStatus(id, status) {
      try {
        const task = this.tasks.find(t => t.id === id)
        await axios.put(`/DeviceTasks/${id}`, {
          ...task,
          status
        })
        // 如果状态为已完成，更新完成时间
        if (status === 'Completed') {
          const updatedTask = this.tasks.find(t => t.id === id)
          updatedTask.completedAt = new Date().toISOString()
        }
      } catch (error) {
        console.error('更新任务状态失败:', error)
      }
    },
    // 删除任务
    async deleteTask(id) {
      try {
        await axios.delete(`/DeviceTasks/${id}`)
        this.tasks = this.tasks.filter(task => task.id !== id)
      } catch (error) {
        console.error('删除任务失败:', error)
      }
    },
    // 格式化日期
    formatDate(dateString) {
      if (!dateString) return ''
      const date = new Date(dateString)
      return date.toLocaleString('zh-CN')
    }
  }
}
</script>

<style scoped>
.device-tasks {
  margin-top: 20px;
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

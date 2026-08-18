<template>
  <div class="add-agv-task">
    <div class="header">
      <h2>添加AGV任务</h2>
      <button class="btn-back" @click="goHome">
        <span class="back-icon">←</span> 返回
      </button>
    </div>
    
    <div class="task-form">
      <form @submit.prevent="addTask">
        <div class="form-group">
          <label for="fromLocation">起始位置:</label>
          <div class="input-group">
            <input type="text" id="fromLocation" ref="fromLocationInput" v-model="newTask.fromLocation" required placeholder="例如：A1-01-01">
            <button type="button" class="btn-clear" @click="clearFromLocation">
              ×
            </button>
          </div>
        </div>
        <div class="form-group">
          <label for="toLocation">目标位置:</label>
          <input type="text" id="toLocation" v-model="newTask.toLocation" disabled>
        </div>
        <div v-if="successMessage" class="success-message">
          {{ successMessage }}
        </div>
        <div v-if="errorMessage" class="error-message">
          {{ errorMessage }}
        </div>
        <button type="submit" class="btn">添加任务</button>
      </form>
    </div>
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'AddAgvTaskView',
  data() {
    return {
      newTask: {
        fromLocation: '',
        toLocation: 'LK02' // 目标位置固定为LK02
      },
      successMessage: '',
      errorMessage: ''
    }
  },
  mounted() {
    // 组件加载时自动聚焦，但尝试避免弹出虚拟键盘
    this.$nextTick(() => {
      // 使用setTimeout延迟设置焦点，可能会减少虚拟键盘弹出的概率
      setTimeout(() => {
        if (this.$refs.fromLocationInput) {
          this.$refs.fromLocationInput.focus({ preventScroll: true })
        }
      }, 100)
    })
  },
  methods: {
    goHome() {
      this.$router.push('/')
    },
    clearFromLocation() {
      this.newTask.fromLocation = ''
      // 清空后自动聚焦，但尝试避免弹出虚拟键盘
      this.$nextTick(() => {
        setTimeout(() => {
          if (this.$refs.fromLocationInput) {
            this.$refs.fromLocationInput.focus({ preventScroll: true })
          }
        }, 100)
      })
    },
    async addTask() {
      try {
        // 清空之前的消息
        this.successMessage = ''
        this.errorMessage = ''
        
        // 验证表单数据
        if (!this.newTask.fromLocation) {
          this.errorMessage = '请填写起始位置'
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
        
        // 显示成功消息
        this.successMessage = '任务添加成功！'
        
        // 3秒后自动清除成功消息
        setTimeout(() => {
          this.successMessage = ''
        }, 3000)
        
        // 重置表单
        this.newTask = {
          fromLocation: '',
          toLocation: 'LK02'
        }
        
        // 任务创建成功后自动聚焦到起始位置输入框，准备下一次输入，但尝试避免弹出虚拟键盘
        this.$nextTick(() => {
          setTimeout(() => {
            if (this.$refs.fromLocationInput) {
              this.$refs.fromLocationInput.focus({ preventScroll: true })
            }
          }, 100)
        })
      } catch (error) {
        console.error('添加任务失败:', error)
        this.errorMessage = '添加任务失败: ' + error.message
      }
    }
  }
}
</script>

<style scoped>
.add-agv-task {
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

.task-form {
  background-color: #f9f9f9;
  padding: 15px;
  border-radius: 6px;
  margin-bottom: 15px;
}

.form-group {
  margin-bottom: 12px;
}

label {
  display: block;
  margin-bottom: 4px;
  font-weight: bold;
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

input {
  width: 100%;
  padding: 6px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

input:disabled {
  background-color: #f5f5f5;
  cursor: not-allowed;
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

.error-message {
  background-color: #fee;
  color: #c00;
  padding: 8px;
  border-radius: 4px;
  margin-bottom: 12px;
  border: 1px solid #fcc;
  font-size: 14px;
}

.btn {
  background-color: #4CAF50;
  color: white;
  padding: 8px 12px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  width: 100%;
  margin-top: 10px;
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
</style>
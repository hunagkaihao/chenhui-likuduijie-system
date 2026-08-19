
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

// 创建一个独立的 axios 实例，用于 PDA 物料信息查询
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

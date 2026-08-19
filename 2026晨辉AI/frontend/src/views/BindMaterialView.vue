<template>
  <div class="material-agv-task">
    <div class="header">
      <h2>物料绑定</h2>
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
            <button type="button" class="btn-clear" @click="clearMaterialId">
              ×
            </button>
          </div>
        </div>
        <div v-if="materialError" class="error-message">
          {{ materialError }}
        </div>

        <div class="form-group form-group-inline">
          <label for="palletId">托盘条码:</label>
          <div class="input-group">
            <input type="text" id="palletId" ref="palletInput" v-model="materialForm.palletId" required placeholder="请输入或扫描托盘条码">
            <button type="button" class="btn-clear" @click="clearPalletId">
              ×
            </button>
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
        <div v-if="Object.keys(materialInfo).length > 0" class="material-info">
          <h4>物料信息</h4>
          <div class="info-item">
            <span>名称: </span>
            <span>{{ materialInfo.name || '未知' }}</span>
          </div>
          <div class="info-item">
            <span>规格: </span>
            <span>{{ materialInfo.spec || '未知' }}</span>
          </div>
          <div class="info-item">
            <span>单位: </span>
            <span>{{ materialInfo.unit || '未知' }}</span>
          </div>
        </div>

        <div v-if="successMessage" class="success-message">
          {{ successMessage }}
        </div>
      </div>
    </div>

    <!-- 按钮容器 -->
    <div class="buttons-container">
      <form @submit.prevent="clearAllData" class="button-form">
        <button type="submit" class="btn btn-danger">清空数据</button>
      </form>
      <form @submit.prevent="bindMaterial" class="button-form">
        <button type="submit" class="btn btn-success" :disabled="submitLock">
          {{ submitLock ? '提交中...' : '绑定物料' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script>
import axios from 'axios'

// 创建一个新的 axios 实例，覆盖 baseURL
const pdaAxios = axios.create({
  baseURL: '' // 空字符串，避免添加任何前缀
})

export default {
  name: 'BindMaterialView',
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
      successMessage: '',
      lastRequestTime: 0,
      submitLock: false
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
      this.$nextTick(() => {
        setTimeout(() => {
          if (this.$refs.materialInput) {
            this.$refs.materialInput.focus({ preventScroll: true })
          }
        }, 100)
      })
    },
    clearPalletId() {
      this.materialForm.palletId = ''
      this.$nextTick(() => {
        setTimeout(() => {
          if (this.$refs.palletInput) {
            this.$refs.palletInput.focus({ preventScroll: true })
          }
        }, 100)
      })
    },
    // 处理物料条码输入，自动检测扫描完成
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
    async bindMaterial() {
      if (this.submitLock) return
      this.submitLock = true
      setTimeout(() => { this.submitLock = false }, 2000)
      try {
        this.successMessage = ''
        this.materialError = ''

        if (!this.materialForm.materialId) {
          this.materialError = '请先扫描物料条码'
          return
        }
        if (!this.materialForm.palletId) {
          this.materialError = '请先扫描托盘条码'
          return
        }

        // 根据物料码开头确定公司名
        let company = ''
        const materialId = this.materialForm.materialId
        if (materialId.startsWith('R')) {
          company = '光宝'
        } else if (materialId.startsWith('@RQ')) {
          company = '婴宝'
        }

        const response = await pdaAxios.post('/pda-api/PDA/RK/Bind', {
          biaoshima: this.materialForm.materialId,
          tuopanma: this.materialForm.palletId,
          picima: this.materialForm.picima,
          tepi: this.materialForm.tepi,
          workMan: company,
          company: company,
          shuliang: parseInt(this.materialForm.quantity)
        })

        if (response.data) {
          const result = response.data
          if (typeof result === 'object' && result.reStatus !== undefined) {
            if (result.reStatus === 'E') {
              const msg = result.reInfo || '未知错误'
              this.recordBind(company, 'Failed', msg)
              this.materialError = '绑定失败: ' + msg
              return
            }
          } else if (typeof result === 'object' && result.reInfo) {
            const reInfoParts = result.reInfo.split(':')
            if (reInfoParts[1] === 'N' || result.reInfo.includes('失败')) {
              this.recordBind(company, 'Failed', result.reInfo)
              this.materialError = '绑定失败: ' + result.reInfo
              return
            }
          } else if (Array.isArray(result)) {
            if (result[0] === 'N' || result[0] === 'E') {
              const msg = (result[1] || '') + ' ' + (result[2] || '')
              this.recordBind(company, 'Failed', msg)
              this.materialError = '绑定失败: ' + msg
              return
            }
          }
          console.log('立库绑定成功')
          this.recordBind(company, 'Success', '绑定成功')
          this.successMessage = '绑定成功！'

          this.materialForm = {
            palletId: '',
            materialId: '',
            picima: '',
            tepi: '',
            quantity: 1
          }
          this.materialInfo = {}

          setTimeout(() => {
            this.successMessage = ''
          }, 3000)

          this.$nextTick(() => {
            setTimeout(() => {
              if (this.$refs.materialInput) {
                this.$refs.materialInput.focus({ preventScroll: true })
              }
            }, 100)
          })
        } else {
          this.recordBind(company, 'Failed', '未知错误')
          this.materialError = '绑定失败：未知错误'
        }
      } catch (error) {
        console.error('绑定物料失败:', error)
        this.materialError = '绑定物料失败: ' + (error.response?.data?.message || error.message)
      }
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
            this.$nextTick(() => {
              setTimeout(() => {
                if (this.$refs.palletInput) {
                  this.$refs.palletInput.focus({ preventScroll: true })
                }
              }, 100)
            })
          } else if (result[0] === 'N') {
            this.materialError = result[1] + result[2]
            this.materialInfo = {}
            this.materialForm.materialId = ''
          } else if (result[0] === 'R') {
            this.materialError = result[1] + result[2]
            this.materialInfo = {}
            this.materialForm.materialId = ''
          } else if (result[0] === 'E') {
            this.materialError = result[1] + (result[2] || '')
            this.materialInfo = {}
            this.materialForm.materialId = ''
          } else {
            this.materialError = '获取物料信息失败：未知返回值'
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
    clearAllData() {
      this.materialForm = {
        palletId: '',
        materialId: '',
        picima: '',
        tepi: '',
        quantity: 1
      }
      this.materialInfo = {}
      this.materialError = ''
      this.successMessage = ''
      this.$nextTick(() => {
        setTimeout(() => {
          if (this.$refs.materialInput) {
            this.$refs.materialInput.focus({ preventScroll: true })
          }
        }, 100)
      })
    },
    async recordBind(company, status, message) {
      try {
        await axios.post('/BindRecords', {
          biaoshima: this.materialForm.materialId,
          tuopanma: this.materialForm.palletId,
          picima: this.materialForm.picima,
          tepi: this.materialForm.tepi,
          workMan: company,
          company: company,
          shuliang: parseInt(this.materialForm.quantity),
          status: status,
          message: message
        })
        console.log('绑定记录已保存:', status, message)
      } catch (error) {
        console.error('保存绑定记录失败:', error.response?.data || error.message)
      }
    }
  }
}
</script>

<style scoped>
.material-agv-task {
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

.section h3 {
  margin-bottom: 10px;
  font-size: 16px;
  color: #333;
  border-bottom: 1px solid #ddd;
  padding-bottom: 5px;
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

.scan-form,
.task-form,
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

input:disabled {
  background-color: #f5f5f5;
  cursor: not-allowed;
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

/* 按钮容器样式 */
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

<template>
  <div class="material-scan">
    <div class="header">
      <h2>物料绑定</h2>
      <button class="btn-back" @click="goHome">
        <span class="back-icon">←</span> 返回
      </button>
    </div>
    
    <div class="scan-form">
      <form @submit.prevent="bindMaterial">
        <div class="form-group">
          <label for="materialId">物料条码:</label>
          <div class="input-group">
            <input type="text" id="materialId" ref="materialInput" v-model="formData.materialId" required placeholder="请扫描物料条码" @keydown.enter.prevent="scanMaterial()" @input="handleMaterialInput">
            <button type="button" class="btn-clear" @click="clearMaterialId">
              ×
            </button>
          </div>
        </div>
        <div v-if="errorMessage" class="error-message">
          {{ errorMessage }}
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
        <div class="form-group">
          <label for="palletId">托盘条码:</label>
          <div class="input-group">
            <input type="text" id="palletId" ref="palletInput" v-model="formData.palletId" required placeholder="请输入或扫描托盘条码" @keydown.enter.prevent="bindMaterial()" @input="handlePalletInput">
            <button type="button" class="btn-clear" @click="clearPalletId">
              ×
            </button>
          </div>
        </div>
        <button type="submit" class="btn">绑定物料</button>
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
  name: 'MaterialScanView',
  data() {
    return {
      formData: {
        palletId: '',
        materialId: '',
        quantity: 1
      },
      materialInfo: {},
      errorMessage: '',
      lastRequestTime: 0
    }
  },
  mounted() {
    // 组件加载时自动聚焦，但尝试避免弹出虚拟键盘
    this.$nextTick(() => {
      // 使用setTimeout延迟设置焦点，可能会减少虚拟键盘弹出的概率
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
      this.formData.materialId = ''
      this.materialInfo = {}
      this.errorMessage = ''
      // 清空后自动聚焦，但尝试避免弹出虚拟键盘
      this.$nextTick(() => {
        setTimeout(() => {
          if (this.$refs.materialInput) {
            this.$refs.materialInput.focus({ preventScroll: true })
          }
        }, 100)
      })
    },
    clearPalletId() {
      this.formData.palletId = ''
      // 清空后自动聚焦，但尝试避免弹出虚拟键盘
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
      // 清除之前的定时器
      if (this.materialInputTimer) {
        clearTimeout(this.materialInputTimer)
      }
      
      // 设置新的定时器，1000ms内无输入则认为扫描完成
      this.materialInputTimer = setTimeout(() => {
        if (this.formData.materialId && this.formData.materialId.length > 0) {
          this.scanMaterial()
        }
      }, 1000)
    },
    // 处理托盘条码输入，自动检测扫描完成
    handlePalletInput() {
      // 清除之前的定时器
      if (this.palletInputTimer) {
        clearTimeout(this.palletInputTimer)
      }
      
      // 设置新的定时器，300ms内无输入则认为扫描完成
      this.palletInputTimer = setTimeout(() => {
        if (this.formData.palletId && this.formData.palletId.length > 0) {
          this.bindMaterial()
        }
      }, 300)
    },
    async bindMaterial() {
      // 节流限制：1秒内只能请求一次
      const currentTime = Date.now()
      if (currentTime - this.lastRequestTime < 1000) {
        return
      }
      
      try {
        // 清空之前的错误信息
        this.errorMessage = ''
        
        // 根据物料码开头确定公司名
        let company = '晨辉'
        const materialId = this.formData.materialId
        if (materialId.startsWith('R')) {
          company = '光宝'
        } else if (materialId.startsWith('@RQ')) {
          company = '婴宝'
        }
        
        // 更新上次请求时间
        this.lastRequestTime = currentTime
        
        const response = await pdaAxios.post('/pda-api/PDA/RK/Bind', {
          biaoshima: this.formData.materialId,
          tuopanma: this.formData.palletId,
          picima: '',
          tepi: '',
          workMan: company,
          company: company,
          shuliang: parseInt(this.formData.quantity)
        })
        
        if (response.data) {
          const result = response.data
          let message = ''
          
          // 检查是否是新的响应格式 {reStatus: "S", reInfo: "失败:N:无合法记录！"} 或 {reStatus: "S", reInfo: "失败::"}
          if (typeof result === 'object' && result.reInfo) {
            const reInfoParts = result.reInfo.split(':')
            if (reInfoParts[1] === 'N' || result.reInfo.includes('失败')) {
              // 显示错误信息在页面上
              let errorMessage = reInfoParts.slice(2).join(':')
              if (!errorMessage) {
                errorMessage = '绑定失败'
              }
              this.errorMessage = errorMessage
              return
            } else {
              message = '绑定成功！'
            }
          } else if (Array.isArray(result)) {
            // 旧的响应格式数组
            if (result[0] === 'N') {
              // 显示错误信息在页面上
              this.errorMessage = result[1] + ' ' + result[2]
              return
            } else {
              message = '绑定成功！'
            }
          } else {
            message = '绑定成功！'
          }
          
          // 显示成功提示
          alert(message)
          
          // 重置表单
          this.formData = {
            palletId: '',
            materialId: '',
            quantity: 1
          }
          
          // 清空物料信息
          this.materialInfo = {}
          
          // 绑定成功后自动聚焦到物料输入框，准备下一次扫描，但尝试避免弹出虚拟键盘
          this.$nextTick(() => {
            setTimeout(() => {
              if (this.$refs.materialInput) {
                this.$refs.materialInput.focus({ preventScroll: true })
              }
            }, 100)
          })
        } else {
          this.errorMessage = '绑定失败：未知错误'
        }
      } catch (error) {
        console.error('绑定物料失败:', error)
        this.errorMessage = '绑定物料失败，请检查网络连接或API地址'
      }
    },
    async scanMaterial() {
  // 节流限制：1秒内只能请求一次
  const currentTime = Date.now()
  if (currentTime - this.lastRequestTime < 1000) {
    return
  }
  
  if (!this.formData.materialId) {
    this.errorMessage = '请输入物料条码'
    return
  }
  
  // 清空之前的物料信息和错误信息
  this.materialInfo = {}
  this.errorMessage = ''
  
  // 根据物料码开头确定公司名
  let company = '晨辉'
  const materialId = this.formData.materialId
  if (materialId.startsWith('R')) {
    company = '光宝'
  } else if (materialId.startsWith('@RQ')) {
    company = '婴宝'
  }
  
  try {
    // 更新上次请求时间
    this.lastRequestTime = currentTime
    
    const response = await pdaAxios.get('/pda-api/PDA/RK/getBSMData', {
      params: {
        biaoshima: this.formData.materialId,
        company: company
      }
    })
    
    if (response.data) {
          // 解析返回的数组
          const result = response.data
          
          if (result[0] === 'N') {
            // 显示错误信息
            this.errorMessage = result[1] + result[2]
          } else {
            // 清除错误信息
            this.errorMessage = ''
            // 假设返回的数据结构中包含物料信息
            this.materialInfo = {
              name: result[1] || '未知',
              spec: result[2] || '未知',
              unit: result[3] || '未知'
            }
            // 扫描成功后自动聚焦到托盘输入框，但尝试避免弹出虚拟键盘
            this.$nextTick(() => {
              setTimeout(() => {
                if (this.$refs.palletInput) {
                  this.$refs.palletInput.focus({ preventScroll: true })
                }
              }, 100)
            })
          }
        } else {
          this.errorMessage = '获取物料信息失败：未知错误'
        }
  } catch (error) {
    console.error('扫描物料条码失败:', error)
    this.errorMessage = '扫描物料条码失败，请检查网络连接或API地址'
  }
}
  }
}
</script>

<style scoped>
.material-scan {
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

.error-message {
  background-color: #fee;
  color: #c00;
  padding: 8px;
  border-radius: 4px;
  margin-bottom: 10px;
  border: 1px solid #fcc;
  font-size: 12px;
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

.scan-form {
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
  display: flex;
  gap: 8px;
}

.input-group input {
  flex: 1;
}

.input-group .btn {
  white-space: nowrap;
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

.info-item span:first-child {
  font-weight: bold;
  margin-right: 8px;
}


</style>

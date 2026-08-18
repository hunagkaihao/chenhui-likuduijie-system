<template>
  <div class="cells">
    <h2>Cell管理</h2>
    
    <!-- 添加Cell表单 -->
    <div class="cell-form">
      <h3>添加库位</h3>
      <form @submit.prevent="addCell">
        <div class="form-group">
          <label for="cellCode">库位编码:</label>
          <input type="text" id="cellCode" v-model="newCell.cellCode" required placeholder="例如：CELL001">
        </div>
        <div class="form-group">
          <label for="cellType">库位类型:</label>
          <input type="text" id="cellType" v-model="newCell.cellType" required placeholder="例如：存储、工作站等">
        </div>
        <div class="form-group">
          <label for="location">位置:</label>
          <input type="text" id="location" v-model="newCell.location" placeholder="例如：A区1排2层">
        </div>
        <div class="form-group">
          <label for="zone">区域:</label>
          <input type="text" id="zone" v-model="newCell.zone" placeholder="例如：A区">
        </div>
        <div class="form-group">
          <label for="shelfCode">货架编码:</label>
          <input type="text" id="shelfCode" v-model="newCell.shelfCode" placeholder="例如：SHELF001">
        </div>
        <div class="form-group">
          <label for="description">描述:</label>
          <textarea id="description" v-model="newCell.description" placeholder="库位详细描述"></textarea>
        </div>
        <button type="submit" class="btn">添加库位</button>
      </form>
    </div>
    
    <!-- Cell绑定表单 -->
    <div class="bind-form">
      <h3>库位绑定</h3>
      <form @submit.prevent="bindCell">
        <div class="form-group">
          <label for="bindCellCode">库位编码:</label>
          <input type="text" id="bindCellCode" v-model="bindForm.cellCode" required placeholder="例如：CELL001">
        </div>
        <div class="form-group">
          <label for="materialCode">物料编码:</label>
          <input type="text" id="materialCode" v-model="bindForm.materialCode" required placeholder="例如：MAT001">
        </div>
        <div class="form-group">
          <label for="quantity">数量:</label>
          <input type="number" id="quantity" v-model="bindForm.quantity" required min="1" value="1">
        </div>
        <button type="submit" class="btn">绑定物料</button>
        <button type="button" class="btn btn-secondary" @click="unbindCell">解绑物料</button>
      </form>
    </div>
    
    <!-- Cell列表 -->
    <div class="cell-list">
      <h3>库位列表</h3>
      <table>
        <thead>
          <tr>
            <th>库位编码</th>
            <th>库位类型</th>
            <th>状态</th>
            <th>位置</th>
            <th>区域</th>
            <th>货架编码</th>
            <th>绑定物料</th>
            <th>绑定数量</th>
            <th>创建时间</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="cell in cells" :key="cell.id">
            <td>{{ cell.cellCode }}</td>
            <td>{{ cell.cellType }}</td>
            <td>{{ cell.status }}</td>
            <td>{{ cell.location || '-' }}</td>
            <td>{{ cell.zone || '-' }}</td>
            <td>{{ cell.shelfCode || '-' }}</td>
            <td>{{ cell.bindMaterialCode || '-' }}</td>
            <td>{{ cell.bindQuantity || '-' }}</td>
            <td>{{ formatDate(cell.createdAt) }}</td>
            <td>
              <button class="btn btn-danger" @click="deleteCell(cell.id)">删除</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="cells.length === 0" class="empty-message">
        暂无库位
      </div>
    </div>
  </div>
</template>

<script>
// 库位管理组件
// 功能：
// 1. 添加上新库位
// 2. 绑定物料到库位
// 3. 从库位解绑物料
// 4. 查看库位列表
// 5. 删除库位
import axios from 'axios'

export default {
  name: 'CellsView',
  data() {
    return {
      cells: [], // 库位列表数据
      newCell: { // 新库位表单数据
        cellCode: '', // 库位编码
        cellType: '', // 库位类型
        location: '', // 位置
        zone: '', // 区域
        shelfCode: '', // 货架编码
        description: '' // 描述
      },
      bindForm: { // 库位绑定表单数据
        cellCode: '', // 库位编码
        materialCode: '', // 物料编码
        quantity: 1 // 数量
      }
    }
  },
  mounted() {
    // 组件挂载后获取库位列表
    this.fetchCells()
  },
  methods: {
    // 获取库位列表
    async fetchCells() {
      try {
        const response = await axios.get('/api/Cells')
        this.cells = response.data
      } catch (error) {
        console.error('获取库位列表失败:', error)
      }
    },
    // 添加库位
    async addCell() {
      try {
        const response = await axios.post('/api/Cells', this.newCell)
        this.cells.push(response.data)
        // 重置表单
        this.newCell = {
          cellCode: '',
          cellType: '',
          location: '',
          zone: '',
          shelfCode: '',
          description: ''
        }
      } catch (error) {
        console.error('添加库位失败:', error)
      }
    },
    // 绑定物料到库位
    async bindCell() {
      try {
        await axios.post('/api/Cells/bind', this.bindForm)
        // 刷新库位列表
        this.fetchCells()
        // 重置表单
        this.bindForm = {
          cellCode: '',
          materialCode: '',
          quantity: 1
        }
      } catch (error) {
        console.error('绑定物料失败:', error)
      }
    },
    // 从库位解绑物料
    async unbindCell() {
      try {
        await axios.post('/api/Cells/unbind', { cellCode: this.bindForm.cellCode })
        // 刷新库位列表
        this.fetchCells()
        // 重置表单
        this.bindForm = {
          cellCode: '',
          materialCode: '',
          quantity: 1
        }
      } catch (error) {
        console.error('解绑物料失败:', error)
      }
    },
    // 删除库位
    async deleteCell(id) {
      try {
        await axios.delete(`/api/Cells/${id}`)
        this.cells = this.cells.filter(cell => cell.id !== id)
      } catch (error) {
        console.error('删除库位失败:', error)
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
.cells {
  margin-top: 20px;
}

.cell-form, .bind-form {
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

.btn-secondary {
  background-color: #6c757d;
}

.btn-secondary:hover {
  background-color: #5a6268;
}

.btn-danger {
  background-color: #f44336;
}

.btn-danger:hover {
  background-color: #da190b;
}

.cell-list {
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

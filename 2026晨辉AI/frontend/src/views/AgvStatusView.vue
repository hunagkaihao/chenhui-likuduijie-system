<template>
  <div class="agv-status-board">
    <h2>AGV状态看板</h2>
    <div class="table-container">
      <table id="agv-table">
        <thead>
          <tr>
            <th>楼层</th>
            <th>车号</th>
            <th>电量</th>
            <th>状态</th>
            <th>在线状态</th>
            <th>备注</th>
          </tr>
        </thead>
        <tbody class="scroll-content">
          <tr v-for="(agv, index) in displayAgvData" :key="agv.robotCode + index" :class="{ 'abnormal-row': !agv.online || agv.battery < 30 }">
            <td>{{ agv.mapCode }}</td>
            <td>{{ agv.robotCode }}</td>
            <td>{{ agv.battery }}%</td>
            <td :class="getStatusClass(agv)">
              {{ getStatusText(agv.status) }}
            </td>
            <td :class="agv.online ? 'status-normal' : 'status-abnormal'">
              {{ agv.online ? '在线' : '离线' }}
            </td>
            <td :class="!agv.online || agv.battery < 30 ? 'remark-abnormal' : ''">
              {{ getRemark(agv) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script>
export default {
  name: 'AgvStatusView',
  data() {
    return {
      agvData: [],
      displayAgvData: [],
      loading: false,
      scrollInterval: null
    }
  },
  mounted() {
    // 初始加载数据
    this.fetchAgvStatus();
    // 每1分钟刷新数据
    this.updateInterval = setInterval(this.fetchAgvStatus, 60000);
  },
  beforeUnmount() {
    // 清除定时器
    if (this.updateInterval) {
      clearInterval(this.updateInterval);
    }
    if (this.scrollInterval) {
      clearInterval(this.scrollInterval);
    }
  },
  watch: {
    agvData: {
      handler() {
        this.startScrolling();
      },
      deep: true
    }
  },
  methods: {
    async fetchAgvStatus() {
      try {
        this.loading = true;
        // 调用query-all接口获取所有楼层的AGV状态
        const response = await fetch('/api/AgvStatus/query-all');
        const data = await response.json();
        if (data.code === '0' && data.data) {
          this.agvData = data.data;
        }
      } catch (error) {
        console.error('获取AGV状态失败:', error);
      } finally {
        this.loading = false;
      }
    },
    startScrolling() {
      // 清除之前的滚动定时器
      if (this.scrollInterval) {
        clearInterval(this.scrollInterval);
      }
      
      // 初始化显示数据
      this.displayAgvData = [...this.agvData];
      
      // 开始滚动，每2秒滚动一次
      this.scrollInterval = setInterval(() => {
        this.scrollData();
      }, 2000);
    },
    scrollData() {
      if (this.agvData.length === 0) return;
      
      // 将第一个元素移到末尾，实现循环滚动效果
      const firstElement = this.displayAgvData.shift();
      this.displayAgvData.push(firstElement);
    },
    getStatusClass(agv) {
      if (!agv.online) return 'status-abnormal';
      if (agv.battery < 30) return 'status-abnormal';
      return 'status-normal';
    },
    getStatusText(status) {
      // 根据状态码返回对应的状态文本
      const statusMap = {
        '2': '运行中',
        '246': '待机'
        // 可以根据实际状态码扩展
      };
      return statusMap[status] || status;
    },
    getRemark(agv) {
      if (!agv.online) return '离线';
      if (agv.battery < 30) return '电量低';
      return '';
    }
  }
}
</script>

<style scoped>
.agv-status-board {
  margin-top: 30px;
}

.table-container {
  overflow-x: auto;
  height: 400px;
  border: 1px solid #ddd;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

#agv-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

#agv-table th,
#agv-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #e0e0e0;
}

#agv-table th {
  background-color: #f5f5f5;
  font-weight: bold;
  color: #666;
  position: sticky;
  top: 0;
  z-index: 1;
}

#agv-table tr:hover {
  background-color: #f9f9f9;
}

#agv-table tr.abnormal-row {
  background-color: #fff3f3;
}

#agv-table td:nth-child(4) {
  font-weight: bold;
}

.status-normal {
  color: #52c41a;
}

.status-abnormal {
  color: #ff4d4f;
}

.remark-abnormal {
  color: #ff4d4f;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .table-container {
    height: 300px;
  }
  
  #agv-table {
    font-size: 12px;
  }
  
  #agv-table th,
  #agv-table td {
    padding: 8px;
  }
}
</style>
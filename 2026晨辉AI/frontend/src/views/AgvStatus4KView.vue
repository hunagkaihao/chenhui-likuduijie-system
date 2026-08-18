<template>
  <div class="agv-status-4k-board">
    <h1>AGV状态看板</h1>
    <div class="table-container">
      <table class="agv-table">
        <thead>
          <tr>
            <th class="col-floor">楼层</th>
            <th class="col-car">车号</th>
            <th class="col-battery">电量</th>
            <th class="col-status">状态</th>
            <th class="col-online">在线状态</th>
            <th class="col-remark">备注</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(agv, index) in displayAgvData" :key="agv.robotCode + index" :class="{ 'abnormal-row': !agv.online || agv.battery < 30 }">
            <td class="col-floor">{{ agv.mapCode }}</td>
            <td class="col-car">{{ agv.robotCode }}</td>
            <td class="col-battery">{{ agv.battery }}%</td>
            <td :class="['col-status', getStatusClass(agv)]">
              {{ getStatusText(agv.status) }}
            </td>
            <td :class="['col-online', agv.online ? 'status-normal' : 'status-abnormal']">
              {{ agv.online ? '在线' : '离线' }}
            </td>
            <td :class="['col-remark', !agv.online || agv.battery < 30 ? 'remark-abnormal' : '']">
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
  name: 'AgvStatus4KView',
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
      
      // 错误状态码列表
      const errorStatusCodes = ['3', '10', '11', '12', '13', '14', '15', '16', '17', '18', '19', '20', '21', '22', '23', '24', '25', '26', '27', '28', '29', '30', '31', '32', '33', '34', '35', '36', '37', '38', '39', '41', '42', '43', '44', '45', '46', '56', '57', '58', '59', '60', '62', '64', '65', '66', '79', '80', '81', '82', '83', '84', '85', '86', '87', '88', '89', '90', '91', '92', '93', '94', '95', '96', '97', '98', '99', '100', '101', '102', '103', '104', '105', '109', '111', '112', '113', '114', '115', '117', '118', '119', '121', '122', '130', '150', '152', '153', '154', '155', '156', '160', '161', '205', '206', '207', '208', '209', '210', '211', '212', '213', '214', '215', '216', '217', '218', '219', '220', '221', '222', '223', '224', '225', '226', '227', '228', '229', '230', '231', '232', '233', '234', '235', '236', '240', '241', '242', '243', '249', '250', '251', '252', '253', '254', '255', '260', '261', '262', '263', '300', '301', '302', '400'];
      if (errorStatusCodes.includes(agv.status)) {
        return 'status-abnormal';
      }
      
      return 'status-normal';
    },
    getStatusText(status) {
      // 根据状态码返回对应的状态文本
      const statusMap = {
        '1': '任务完成',
        '2': '任务执行中',
        '3': '任务异常',
        '4': '任务空闲',
        '5': '机器人暂停',
        '6': '举升货架状态',
        '7': '充电状态',
        '8': '弧线行走中',
        '9': '充电中(充满维护)',
        '10': '异常状态开始',
        '11': '背货未识别',
        '12': '货架偏角过大',
        '13': '运动库异常',
        '14': '货码无法识别',
        '15': '货码不匹配',
        '16': '举升异常',
        '17': '充电桩异常',
        '18': '电量无增加',
        '19': '充电站失联',
        '20': '充电指令角度错误',
        '21': '平台下发指令错误',
        '22': '货架位置不可信',
        '23': '外力下放',
        '24': '货架位置偏移',
        '25': '小车不在锁定区',
        '26': '下放重试失败',
        '27': '货架摆歪',
        '28': '举升电池电量太低',
        '29': '后退角度偏大',
        '30': '未背货架举升在上',
        '31': '区域锁定失败',
        '32': '充电站未连接',
        '33': '旋转申请暂时失败',
        '34': '地图切换点地码未识别',
        '35': '异常偏航',
        '36': '显示屏操作中',
        '37': '旋转申请永久失败',
        '38': '货架堵转',
        '39': 'indx 39',
        '40': '分拣物件中',
        '41': '异常只能移动，不做业务',
        '42': '翻盖任务单号为0',
        '43': '翻盖任务单号不匹配',
        '44': '翻盖上升超时',
        '45': '翻盖下放超时',
        '46': '线激光标定中',
        '56': '辊筒控制中',
        '57': '对接滚动失败',
        '58': '传动指令错误（有料箱发接，无料箱发送等）',
        '59': '传动超时',
        '60': '料箱个数不匹配',
        '61': '对接微调中',
        '62': '对接微调失败',
        '63': 'AGV作业中',
        '64': '等待复位按钮确认',
        '65': '动作执行失败',
        '66': '货架二维码标定失败',
        '70': '交通管制中',
        '79': '货架下微调次数超限',
        '80': '遇障等状态开始',
        '81': '前方遇障',
        '82': '后方遇障',
        '83': '左侧遇障',
        '84': '右侧遇障',
        '85': 'TOF相机检测到障碍物',
        '86': '空车遇障',
        '87': '左侧叉尖遇障(光电或防撞触发)',
        '88': '右侧叉尖遇障(光电或防撞触发)',
        '89': '遇障等状态结束',
        '90': '平台指令错误开始',
        '91': '举升不在低位空车移动',
        '92': '非轴向移动任务',
        '93': '原地举升位置偏差大',
        '94': '斜线背货架任务',
        '95': '弧线目标个数不匹配',
        '96': '禁止货架下转弯任务',
        '97': '非旋转码上转弯任务',
        '98': '任务角度异常',
        '99': '传感器校验中或MCU未READY',
        '100': '货架信息缺失',
        '101': '平台指令业务对象参数错误',
        '102': '平台指令小车料箱位置错误',
        '103': '平台指令料箱尺寸超限',
        '104': '平台指令伸缩机构伸缩量超限',
        '105': '平台指令和载货状态冲突',
        '109': '一共19个',
        '110': '自检进行中',
        '111': '自检发生异常',
        '112': '零位检测失败',
        '113': 'SDK地图切换中',
        '114': 'SLAM定位失败',
        '115': 'SLAM地图配置中',
        '116': '手动模式',
        '117': '平台告警小车异常',
        '118': '小板升级中',
        '119': '感知库异常',
        '120': '绕障行走中',
        '121': '绕障失败',
        '122': '定位库异常',
        '123': '巡线行走中',
        '130': 'RCU举升维护状态',
        '150': '货物检测异常',
        '151': '小车自由运行中',
        '152': '强制完成状态',
        '153': '栈板识别错误',
        '154': '路径规划错误',
        '155': '持续执行无法完成',
        '156': '下放过程检测到物体',
        '160': '激光检测中',
        '161': '激光检测失败',
        '162': '激光检测完成',
        '200': '原地取放料箱动作状态',
        '201': '提前抬叉动作状态',
        '202': '移动与取放料箱联动动作状态',
        '203': '移动取放货任务移动完成',
        '205': '举升动作执行失败',
        '206': '纵向伸缩动作执行失败',
        '207': '横向伸缩动作执行失败',
        '208': '旋转动作执行失败',
        '209': '拨叉动作执行失败',
        '210': '找货架码失败',
        '211': '对接货架码偏差超限',
        '212': '对接货架码移动失败',
        '213': '料箱个数不匹配',
        '214': '夹报机构中已有料箱',
        '215': '夹报机构中没有料箱',
        '216': '小车储位中已有料箱',
        '217': '小车储位中没有料箱',
        '218': '货架中已有料箱',
        '219': '货架中没有料箱',
        '220': '双目识别料箱姿态失败',
        '221': '双目识别料箱偏差超限',
        '222': '双目对接货架料箱调整失败',
        '223': '滚筒动作执行失败',
        '224': '滚筒机构中已有料箱',
        '225': '滚筒机构中没有料箱',
        '226': '找料箱码失败',
        '227': '料箱ID错误',
        '228': '执行机构遇障',
        '229': '拨叉状态异常',
        '230': '小车角度调整失败',
        '231': '举升动作逻辑冲突',
        '232': '双目识别料箱尺寸超限',
        '233': '恢复时小车状态发生改变',
        '234': '储位料箱未知',
        '235': '储位料箱丢失',
        '236': 'CTU联动类型错误',
        '240': '取消恢复任务请求中',
        '241': '信标识别动作执行失败',
        '242': 'CTU料盘状态异常',
        '243': '平台下发目标料箱信息错误',
        '246': '待机模式中',
        '247': '低功耗模式中',
        '248': '休眠模式中',
        '249': '异常休眠模式',
        '250': '电量过低预警',
        '251': 'AGV换电中',
        '252': 'AGV换电失败',
        '253': 'AGV唤醒失败',
        '254': 'AGV进入休眠失败',
        '255': 'AGV电池延时重启失败',
        '260': '弧线移动时前方遇障',
        '261': '弧线后方遇障',
        '262': '弧线左侧遇障',
        '263': '弧线右侧遇障',
        '300': '空车状态检测异常',
        '301': '载货状态检测异常',
        '302': '叉车栈板识别失败',
        '400': '设备不在锁定区'
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
/* 全局样式重置 */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

/* 4K屏幕优化 */
.agv-status-4k-board {
  width: 100vw;
  height: 100vh;
  padding: 40px;
  background-color: #f0f2f5;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* 标题样式 */
h1 {
  font-size: 48px;
  font-weight: bold;
  color: #333;
  text-align: center;
  margin-bottom: 40px;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.1);
}



/* 表格容器 */
.table-container {
  flex: 1;
  overflow: auto;
  background-color: #ffffff;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border: 1px solid #e0e0e0;
}

/* 表格样式 */
.agv-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 24px;
}

.agv-table th,
.agv-table td {
  padding: 24px;
  text-align: left;
  border-bottom: 1px solid #e0e0e0;
}

.agv-table th {
  background-color: #f5f5f5;
  font-weight: bold;
  color: #333;
  position: sticky;
  top: 0;
  z-index: 10;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.agv-table tr:hover {
  background-color: #f9f9f9;
}

.agv-table tr.abnormal-row {
  background-color: #fff3f3;
}

/* 列宽设置 */
.col-floor {
  width: 200px;
}

.col-car {
  width: 150px;
}

.col-battery {
  width: 150px;
}

.col-status {
  width: 200px;
  font-weight: bold;
}

.col-online {
  width: 150px;
  font-weight: bold;
}

.col-remark {
  flex: 1;
  min-width: 300px;
}

/* 状态样式 */
.status-normal {
  color: #52c41a;
  font-weight: bold;
}

.status-abnormal {
  color: #ff4d4f;
  font-weight: bold;
}

.remark-abnormal {
  color: #ff4d4f;
  font-weight: 500;
}

/* 滚动条样式 */
.table-container::-webkit-scrollbar {
  width: 12px;
  height: 12px;
}

.table-container::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 6px;
}

.table-container::-webkit-scrollbar-thumb {
  background: #888;
  border-radius: 6px;
}

.table-container::-webkit-scrollbar-thumb:hover {
  background: #555;
}

/* 响应式设计 */
@media (max-width: 3840px) {
  .agv-status-4k-board {
    padding: 30px;
  }
  
  h1 {
    font-size: 40px;
  }
  
  .status-summary {
    padding: 25px;
  }
  
  .summary-item .label {
    font-size: 20px;
  }
  
  .summary-item .count {
    font-size: 40px;
  }
  
  .agv-table {
    font-size: 20px;
  }
  
  .agv-table th,
  .agv-table td {
    padding: 20px;
  }
}

@media (max-width: 2560px) {
  .agv-status-4k-board {
    padding: 20px;
  }
  
  h1 {
    font-size: 32px;
  }
  
  .status-summary {
    padding: 20px;
  }
  
  .summary-item .label {
    font-size: 16px;
  }
  
  .summary-item .count {
    font-size: 32px;
  }
  
  .agv-table {
    font-size: 18px;
  }
  
  .agv-table th,
  .agv-table td {
    padding: 16px;
  }
}
</style>
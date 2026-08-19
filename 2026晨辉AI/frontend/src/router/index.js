import { createRouter, createWebHashHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import DeviceTasksView from '../views/DeviceTasksView.vue'
import MaterialScanView from '../views/MaterialScanView.vue'
import AddAgvTaskView from '../views/AddAgvTaskView.vue'
import MaterialAndAgvTaskView from '../views/MaterialAndAgvTaskView.vue'
import BindMaterialView from '../views/BindMaterialView.vue'
import AgvTaskListView from '../views/AgvTaskListView.vue'
import CellsView from '../views/CellsView.vue'
import AgvStatusView from '../views/AgvStatusView.vue'
import AgvStatus4KView from '../views/AgvStatus4KView.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: HomeView
  },
  {
    path: '/device-tasks',
    name: 'DeviceTasks',
    component: DeviceTasksView
  },
  {
    path: '/material-scan',
    name: 'MaterialScan',
    component: MaterialScanView
  },
  {
    path: '/add-agv-task',
    name: 'AddAgvTask',
    component: AddAgvTaskView
  },
  {
    path: '/agv-task-list',
    name: 'AgvTaskList',
    component: AgvTaskListView
  },
  {
    path: '/cells',
    name: 'Cells',
    component: CellsView
  },
  {
    path: '/agv-status',
    name: 'AgvStatus',
    component: AgvStatusView
  },
  {
    path: '/agv-status-4k',
    name: 'AgvStatus4K',
    component: AgvStatus4KView
  },
  {
    path: '/material-agv-task',
    name: 'MaterialAndAgvTask',
    component: MaterialAndAgvTaskView
  },
  {
    path: '/bind-material',
    name: 'BindMaterial',
    component: BindMaterialView
  },
  // 捕获所有路由，重定向到首页
  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

export default router

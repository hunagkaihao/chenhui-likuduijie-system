<template>
  <div :class="['app', isBoardRoute ? 'app-board' : 'app-pda']">
    <h1 v-if="$route.path === '/'">立库对接系统</h1>
    <router-view />
  </div>
</template>

<script>
export default {
  name: 'App',
  computed: {
    isBoardRoute() {
      // 看板相关路由使用全屏布局
      return [
        '/agv-status',
        '/agv-status-4k'
      ].includes(this.$route.path);
    }
  }
}
</script>

<style>
/* PDA设备布局 */
.app-pda {
  width: 300px;
  height: 400px;
  margin: 0 auto;
  padding: 10px;
  font-family: Arial, sans-serif;
  overflow: auto;
  box-sizing: border-box;
  position: relative;
}

/* 看板布局 */
.app-board {
  width: 100vw;
  height: 100vh;
  margin: 0;
  padding: 0;
  font-family: Arial, sans-serif;
  overflow: hidden;
  box-sizing: border-box;
  position: relative;
}

h1 {
  color: #333;
  text-align: center;
  font-size: 18px;
  margin-top: 0;
  margin-bottom: 15px;
}

/* 全局样式调整 */
h2 {
  font-size: 16px;
  margin: 0 0 15px 0;
}

button {
  font-size: 12px;
  padding: 6px 12px;
}

input {
  font-size: 14px;
  padding: 6px;
}

/* 看板页面的特殊样式 */
.app-board h1 {
  font-size: 36px;
  margin-bottom: 30px;
}

.app-board h2 {
  font-size: 24px;
  margin-bottom: 30px;
}
</style>

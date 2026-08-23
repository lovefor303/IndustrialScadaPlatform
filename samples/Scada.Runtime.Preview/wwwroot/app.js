(function () {
  "use strict";

  const projectName = document.getElementById("project-name");
  const qualityIndicator = document.getElementById("quality-indicator");
  const screenSelector = document.getElementById("screen-selector");
  const screenStatus = document.getElementById("screen-status");
  const diagnostic = document.getElementById("diagnostic");
  const diagnosticCount = document.getElementById("diagnostic-count");
  const scene = document.getElementById("scene");
  const loginPanel = document.getElementById("login-panel");
  const loginForm = document.getElementById("login-form");
  const loginStatus = document.getElementById("login-status");
  const runtimeStatus = document.getElementById("runtime-status");
  let hub = null;

  function showDiagnostic(message) {
    diagnostic.textContent = message;
    diagnostic.hidden = false;
    screenStatus.textContent = "运行时诊断";
  }

  function showLogin(message) {
    loginPanel.hidden = false;
    loginStatus.textContent = message || "需要登录后查看画面";
    runtimeStatus.textContent = "等待登录，现场命令已禁用。";
  }

  function clearDiagnostic() {
    diagnostic.textContent = "";
    diagnostic.hidden = true;
  }

  async function getJson(url) {
    const response = await fetch(url, { headers: { "Accept": "application/json" } });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      const error = new Error(payload.message || "运行时请求失败。");
      error.code = payload.code || "runtime.http";
      error.status = response.status;
      if (response.status === 401) showLogin("需要登录后查看画面");
      throw error;
    }
    return payload;
  }

  function renderScreen(screen) {
    const bounds = screen.designBounds || { x: 0, y: 0, width: 1920, height: 1080 };
    scene.setAttribute("viewBox", `${bounds.x} ${bounds.y} ${bounds.width} ${bounds.height}`);
    scene.innerHTML = (screen.objects || [])
      .filter(object => object.isVisible !== false)
      .map(object => object.svg || "")
      .join("");
    const diagnostics = screen.diagnostics || [];
    diagnosticCount.textContent = `诊断 ${diagnostics.length}`;
    const hasBad = (screen.objects || []).some(object => String(object.quality).toLowerCase() === "bad" || object.quality === 2);
    qualityIndicator.textContent = hasBad ? "质量异常" : "质量正常";
    qualityIndicator.className = `indicator ${hasBad ? "indicator-bad" : "indicator-good"}`;
    screenStatus.textContent = `当前画面：${screen.name}`;
  }

  async function loadScreen(name) {
    if (!name) return;
    try {
      clearDiagnostic();
      renderScreen(await getJson(`/api/runtime/screens/${encodeURIComponent(name)}`));
    } catch (error) {
      scene.innerHTML = "";
      showDiagnostic(`[${error.code}] ${error.message}`);
      diagnosticCount.textContent = "诊断 1";
      qualityIndicator.textContent = "质量未知";
      qualityIndicator.className = "indicator indicator-unknown";
    }
  }

  async function loadProject() {
    try {
      const project = await getJson("/api/runtime/project");
      projectName.textContent = project.name || "未命名项目";
      screenSelector.innerHTML = "";
      (project.screens || []).forEach(name => {
        const option = document.createElement("option");
        option.value = name;
        option.textContent = name;
        screenSelector.appendChild(option);
      });
      screenSelector.disabled = false;
      screenSelector.addEventListener("change", () => loadScreen(screenSelector.value));
      await loadScreen(screenSelector.value);
      await connectLive(project.screens?.[0], project.variables || []);
    } catch (error) {
      showDiagnostic(`[${error.code}] ${error.message}`);
      qualityIndicator.textContent = "质量未知";
      qualityIndicator.className = "indicator indicator-unknown";
    }
  }

  async function connectLive(screenName, variables) {
    if (!window.signalR || !screenName) return;
    hub = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/runtime")
      .withAutomaticReconnect([0, 1000, 3000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
    hub.onreconnecting(() => {
      runtimeStatus.textContent = "连接中断，正在重连，现场命令已禁用。";
      qualityIndicator.textContent = "质量未知";
      qualityIndicator.className = "indicator indicator-unknown";
    });
    hub.onreconnected(async () => {
      runtimeStatus.textContent = "已重连，正在恢复实时订阅。";
      await hub.invoke("RequestFullSnapshot");
    });
    hub.onclose(() => {
      runtimeStatus.textContent = "实时连接已断开，现场命令已禁用。";
    });
    hub.on("snapshot", snapshot => {
      const values = snapshot.values || [];
      const bad = values.some(value => String(value.quality).toLowerCase() === "bad" || value.quality === 2);
      qualityIndicator.textContent = bad ? "质量异常" : "质量正常";
      qualityIndicator.className = `indicator ${bad ? "indicator-bad" : "indicator-good"}`;
      runtimeStatus.textContent = `实时连接正常，序号 ${snapshot.sequence}`;
    });
    hub.on("diagnostic", message => showDiagnostic(`[${message.code}] ${message.message}`));
    try {
      await hub.start();
      await hub.invoke("Subscribe", screenName, variables);
      runtimeStatus.textContent = "实时连接正常，现场命令已禁用。";
    } catch (error) {
      runtimeStatus.textContent = "实时连接不可用，现场命令已禁用。";
    }
  }

  loginForm.addEventListener("submit", async event => {
    event.preventDefault();
    loginStatus.textContent = "正在登录...";
    const body = {
      userName: document.getElementById("login-user").value,
      password: document.getElementById("login-password").value
    };
    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json", "Accept": "application/json" },
        body: JSON.stringify(body)
      });
      const payload = await response.json().catch(() => ({}));
      document.getElementById("login-password").value = "";
      if (!response.ok) throw new Error(payload.message || "登录失败。");
      loginPanel.hidden = true;
      await loadProject();
    } catch (error) {
      loginStatus.textContent = error.message;
    }
  });

  loadProject();
}());

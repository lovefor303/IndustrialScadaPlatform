(function () {
  "use strict";

  const projectName = document.getElementById("project-name");
  const qualityIndicator = document.getElementById("quality-indicator");
  const screenSelector = document.getElementById("screen-selector");
  const screenStatus = document.getElementById("screen-status");
  const diagnostic = document.getElementById("diagnostic");
  const diagnosticCount = document.getElementById("diagnostic-count");
  const scene = document.getElementById("scene");

  function showDiagnostic(message) {
    diagnostic.textContent = message;
    diagnostic.hidden = false;
    screenStatus.textContent = "运行时诊断";
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
    } catch (error) {
      showDiagnostic(`[${error.code}] ${error.message}`);
      qualityIndicator.textContent = "质量未知";
      qualityIndicator.className = "indicator indicator-unknown";
    }
  }

  loadProject();
}());

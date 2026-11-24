# Jarvis Launcher

一个可以独立运行的贾维斯风格启动器。动画开场后，可以点「说话」或用文字输入「你好，贾维斯」。它会回答「我在」，等你说「继续启动」或点击返回。随项目附带开机视频和剪辑分轨。

## Windows 快速开始

在 Windows x64 上解压完整安装包，双击 `plugins/jarvis-console/Jarvis.exe`。如果下载的是源码，先用 PowerShell 在项目目录运行 `.\build.ps1`，然后运行生成的文件。

退出开机窗口便可回到原来的应用。已经安装 ChatGPT 桌面版的用户，可使用 `--open-chatgpt` 参数在开机动画后自动打开它。语音识别需要 Windows 中文识别器和可用的麦克风，文字和按钮可以直接使用。

## 连接支持本地 MCP 的 AI 客户端

需要 Python 3.10 或更新版本。Windows 上在项目目录运行：

```powershell
py -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

在客户端添加一个 **stdio** 类型的 MCP 服务。下面是常见的 JSON 格式。把两个路径换成你电脑上的绝对路径。

```json
{
  "mcpServers": {
    "jarvis": {
      "command": "C:/Projects/jarvis-launcher/.venv/Scripts/python.exe",
      "args": ["C:/Projects/jarvis-launcher/mcp_server.py"]
    }
  }
}
```

Codex CLI 也可以用下面的命令添加同一个 MCP 服务：

```powershell
codex mcp add jarvis -- "C:/Projects/jarvis-launcher/.venv/Scripts/python.exe" "C:/Projects/jarvis-launcher/mcp_server.py"
```

重载客户端后，试着说「播放贾维斯开机动画」。MCP 提供 `jarvis_status`（检查本机功能）、`jarvis_launch`（播放或打开控制台）和 `jarvis_editing_assets`（查看剪辑包路径）。它在本机运行，启动和查看素材不需要 API 密钥。

完整互动目前面向 Windows。macOS 和 Linux 上可用同一个 MCP 服务打开视频和查看分轨路径；创建虚拟环境可用 `python3 -m venv .venv`，将解释器路径改为 `.venv/bin/python`。只支持远程 MCP 的服务和没有本地工具接口的 AI 网页，无法用这个连接方式打开用户电脑上的窗口。仍可手动播放 `plugins/jarvis-console/assets/boot.mp4`。

## 作为 Codex 本地插件安装

先构建 Windows 启动器，然后在仓库根目录运行：

```powershell
codex plugin marketplace add .
codex plugin add jarvis-console@jarvis-launcher
```

开机时说「你好，贾维斯」，或让 Codex 执行「播放贾维斯开机动画」。素材包可以在控制台点「导出素材」，也可以直接复制 `plugins/jarvis-console/assets/edit-kit.zip`。

## 许可与素材来源

启动器、MCP 服务和文档使用 [MIT 许可证](LICENSE)。动画与剪辑包由项目中的程序化画面、生成的反应堆画面、合成普通话对白和自制音效组成。它是独立的同人项目。电影角色名称和产品商标归各自权利人所有。公开包不含原电影、第三方参考视频或 ChatGPT 的官方图标。

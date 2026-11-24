---
name: jarvis-console
description: 启动 Jarvis Launcher 的贾维斯动画或控制台，或导出其剪辑素材。用于用户明确要求启动贾维斯、播放贾维斯开机或导出该项目素材。
---

本文件向上两级为插件目录。Windows 上，只有用户明确要求启动时才打开 Jarvis.exe。无参数进入互动开机，结束后关闭启动窗口。用户要求动画结束后打开 ChatGPT 时添加 `--open-chatgpt`。用 `--console` 打开控制台，用 `--awake` 打开并播放「我在」。

用户要求导出时，复制 `assets/edit-kit.zip` 到用户指定目录或当前工作区的交付目录。使用新文件名以避免覆盖已有文件，返回实际路径。

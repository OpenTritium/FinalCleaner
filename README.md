# FinalCleaner
FinalCleaner 的定位是轻巧快速，简单优雅并具有极客色彩的 Windows 垃圾清理软件。

## 开发技术

使用 C# 开发，界面由 WinUI3 驱动。

## 企划的程序模块

| 名称   | 描述                                                         |
| ------ | ------------------------------------------------------------ |
| Scout  | 卷管理库，能够向上提供卷描述，生成硬盘索引，卷事件处理器（例如设备插拔）等。 |
| Lens   | WinUI 界面，程序入口，同时也是各个类库的中控。               |
| Ranger | 访问文件（夹），向外提供当前文件（夹）的详情，也提供一些文件删除或修改的操作。 |
| 计划中 | 计划中                                                       |

展望中的功能：

- [ ] 完成 Scoop，choco 等包管理器适配
- [ ] 绿色应用路径匹配
- [ ] 运行时，开发工具链管理
- [ ] 卸载残留清理
- [ ] 卷压缩

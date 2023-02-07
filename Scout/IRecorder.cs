namespace Scout
{
    /// <summary>
    /// 异步实现
    /// </summary>
    internal interface IRecorder
    {
        // 为每个卷提供独立的索引生成方法，无参数重载方法默认从初始位开始写
        internal bool GenerateVolumeIndex();
        // 衔接上个卷索引，继续写入数据库
    }
}
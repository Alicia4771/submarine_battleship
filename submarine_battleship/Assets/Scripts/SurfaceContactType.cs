public enum SurfaceContactType
{
    /// <summary>
    /// 種類未設定。
    /// 基本的にはゲーム中の船には使用しない。
    /// </summary>
    Unknown = 0,


    /// <summary>
    /// 敵船。
    /// 現在は雪風モデルを使用する。
    /// 通信傍受の対象。
    /// </summary>
    Enemy = 1,


    /// <summary>
    /// 味方船。
    /// 現在は天城モデルを使用する。
    /// 通信傍受の対象ではない。
    /// </summary>
    Friendly = 2,


    /// <summary>
    /// 敵でも味方でもない船。
    /// 現在は樫野モデルを使用する。
    /// 通信傍受の対象ではない。
    /// </summary>
    Neutral = 3
}
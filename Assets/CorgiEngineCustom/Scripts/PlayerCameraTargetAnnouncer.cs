using UnityEngine;

/// <summary>
/// 生成されたプレイヤー（自分自身）が、シーン上の CameraRigTargetReceiver を探して、
/// 「この Transform をカメラ追従ターゲットにしてね」と通知するためのスクリプトです。
///
/// 目的：
/// - プレイヤー生成（スポーン）とカメラ設定を疎結合にする（将来のFusion2/マルチ対応を見据えた責務分離）
/// - プレイヤーPrefabに固定参照を持たせず、ランタイムでReceiverへ注入する
///
/// 動き：
/// 1) Start() で Receiver を探す
/// 2) 見つからなければ数フレーム（最大 _maxFramesToRetry）リトライ
/// 3) 見つかれば receiver.SetTarget(transform) を呼んで完了
/// </summary>
public sealed class PlayerCameraTargetAnnouncer : MonoBehaviour
{
    [Header("Find Receiver")]
    [SerializeField] private int _maxFramesToRetry = 120; // 2秒相当(60fps想定)
    [SerializeField] private bool _log = true;

    //↓ [暫定スイッチ] Photon Fusion 2 で LocalPlayer 判定に置き換えるまでの検証用
    [Header("Announce Policy (Temporary)")]
    [SerializeField] private bool _announceEnabled = true; 
    // ↑[暫定スイッチ] Photon Fusion 2 で LocalPlayer 判定に置き換えるまでの検証用

    private int _framesTried;

    private void Start()
    {
        // [検証ログ] このAnnouncerが起動した証拠
        if (_log)
        {
            Debug.Log($"[PlayerCameraTargetAnnouncer] Start on '{name}' (maxRetryFrames={_maxFramesToRetry})");
        }

        // 生成直後に探す（見つからなければ数フレーム再試行）
        TryAnnounceOrRetry();
    }

    private void TryAnnounceOrRetry()
    {
        // [将来差し込み口] Photon Fusion 2 で「このクライアントが追従対象を通知してよいか」を判定する
        // 現時点では _announceEnabled でON/OFFできる暫定スイッチ。
        // 将来 Photon Fusion 2 では「LocalPlayer判定」に置き換える。
        if (!ShouldAnnounce())
        {
            if (_log) Debug.Log($"[PlayerCameraTargetAnnouncer] Skip announce by policy. player='{name}'");
            enabled = false;
            return;
        }

        // [検証ログ] 探索トライの証拠
        if (_log)
        {
            Debug.Log($"[PlayerCameraTargetAnnouncer] Try find Receiver... frame={_framesTried + 1}/{_maxFramesToRetry} player='{name}'");
        }

        var receiver = FindFirstObjectByType<CameraRigTargetReceiver>();

        // [検証ログ] 見つかったかどうか
        if (_log)
        {
            Debug.Log(receiver != null
                ? $"[PlayerCameraTargetAnnouncer] Receiver found: '{receiver.name}' (HasValidCamera={receiver.HasValidCamera})"
                : "[PlayerCameraTargetAnnouncer] Receiver NOT found.");
        }

        if (receiver != null && receiver.HasValidCamera)
        {
            // [検証ログ] これから通知する証拠
            if (_log)
            {
                Debug.Log($"[PlayerCameraTargetAnnouncer] Announce target='{name}' to receiver='{receiver.name}'");
            }

            receiver.SetTarget(transform);

            if (_log)
            {
                Debug.Log($"[PlayerCameraTargetAnnouncer] Target set request sent for '{name}'. (done)");
            }

            // 成功したら終了
            enabled = false;
            return;
        }

        // まだ見つからない場合は次フレームへ
        _framesTried++;
        if (_framesTried >= _maxFramesToRetry)
        {
            Debug.LogWarning($"[PlayerCameraTargetAnnouncer] Receiver not found (or invalid camera). Giving up. player='{name}' triedFrames={_framesTried}");
            enabled = false;
            return;
        }

        // 次フレームで再試行
        Invoke(nameof(TryAnnounceOrRetry), 0f);
    }

    /// <summary>
    /// [将来差し込み口] Photon Fusion 2 導入時に「LocalPlayerのみ通知する」等の判定をここに集約する。
    /// 現時点では常に true（=全員通知）
    /// </summary>
    private bool ShouldAnnounce()
    {
        // [将来差し込み口] Photon Fusion 2 の LocalPlayer 判定へ置換予定
        return _announceEnabled;
    }

}

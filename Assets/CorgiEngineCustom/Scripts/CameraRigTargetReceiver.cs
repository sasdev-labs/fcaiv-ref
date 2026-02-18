using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// プレイヤーなどの Transform を受け取り、Cinemachine の追従対象（TrackingTarget）に設定するスクリプトです。
///
/// 目的：
/// - カメラRig側に「ターゲット設定の受け口」を作り、プレイヤー生成側と疎結合にする
/// - 将来のFusion2/マルチ化で「誰を追うか（LocalPlayerのみ等）」の判断をここへ寄せやすくする
///
/// 想定接続：
/// PlayerCameraTargetAnnouncer →（Transform通知）→ CameraRigTargetReceiver → CinemachineCamera.Target.TrackingTarget
/// </summary>
public sealed class CameraRigTargetReceiver : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    [Header("Debug")]
    [SerializeField] private bool _log = true;

    // ↓[暫定スイッチ] Photon Fusion 2 で「追従対象として採用してよいか（LocalPlayerのみ等）」判定に置き換えるまでの検証用
    [Header("Accept Policy (Temporary)")]
    [SerializeField] private bool _acceptEnabled = true;
    // ↑[暫定スイッチ] Photon Fusion 2 で「追従対象として採用してよいか（LocalPlayerのみ等）」判定に置き換えるまでの検証用

    private void Awake()
    {
        // [検証ログ] Receiverが起動している証拠
        if (_log)
        {
            Debug.Log($"[CameraRigTargetReceiver] Awake on '{name}' (HasValidCamera={HasValidCamera})");
        }
    }

    // [検証用] TrackingTarget が「誰か」により書き換えられたかを検出するための前回値。
    // 監視結果が不要になったら、この変数と LateUpdate() は削除してよい。
    private Transform _lastTarget;

    // [検証ログ] 上書き監視ログ
    private void LateUpdate()
    {
        if (!_log || _cinemachineCamera == null) return;

        var current = _cinemachineCamera.Target.TrackingTarget;

        if (current != _lastTarget)
        {
            Debug.Log($"[CameraRigTargetReceiver] TrackingTarget CHANGED by someone: " +
                    $"from {(_lastTarget != null ? _lastTarget.name : "null")} " +
                    $"to {(current != null ? current.name : "null")}");
            _lastTarget = current;
        }
    }

    /// <summary>
    /// PlayerなどのTransformを受け取り、Cinemachineの追従対象として設定する。
    /// （Prefabに参照を固定せず、ランタイムで注入するための受け口）
    /// </summary>
    public void SetTarget(Transform target)
    {

        // [検証ログ] 呼ばれた証拠
        if (_log)
        {
            Debug.Log($"[CameraRigTargetReceiver] SetTarget called. target={(target != null ? $"'{target.name}'" : "null")}");
        }

        if (_cinemachineCamera == null)
        {
            Debug.LogWarning("[CameraRigTargetReceiver] CinemachineCamera is not assigned.");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[CameraRigTargetReceiver] target is null.");
            return;
        }
        
        // [将来差し込み口] Photon Fusion 2 で LocalPlayer のみ採用などの判定をここに集約する
        if (!ShouldAcceptTarget(target))
        {
            if (_log) Debug.Log($"[CameraRigTargetReceiver] Rejected target='{target.name}' by policy.");
            return;
        }

        // [検証ログ] 適用前の状態
        if (_log)
        {
            var before = _cinemachineCamera.Target.TrackingTarget;
            Debug.Log($"[CameraRigTargetReceiver] Before apply: TrackingTarget={(before != null ? $"'{before.name}'" : "null")}");
        }

        // CinemachineCamera の Tracking Target は Transform
        _cinemachineCamera.Target.TrackingTarget = target;

        // [検証ログ] 適用後の状態
        if (_log)
        {
            var after = _cinemachineCamera.Target.TrackingTarget;
            Debug.Log($"[CameraRigTargetReceiver] After apply: TrackingTarget={(after != null ? $"'{after.name}'" : "null")}");
        }

        // [検証ログ]用 _lastTarget を更新
        _lastTarget = _cinemachineCamera.Target.TrackingTarget;// 監視用に同期
    }

    public bool HasValidCamera => _cinemachineCamera != null;

    private void Reset()
    {
        // 同一Prefab内にあるものを自動取得（安全）
        _cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
    }

    /// <summary>
    /// [将来差し込み口] Photon Fusion 2 導入時に「LocalPlayerのみ採用」等の判定をここに集約する。
    /// 現時点では _acceptEnabled による暫定ON/OFFで検証する。
    /// </summary>
    private bool ShouldAcceptTarget(Transform target)
    {
        return _acceptEnabled;
    }

}

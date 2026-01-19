using DG.Tweening;
using N2K;
using UnityEngine;

public class Defender : PoolMember
{
    #region ___ REFERENCES ___
    // EXTERNAL REFERENCES 

    private DefenderManager _defenderManager;

    [Header("=== DEFENDER ===")]

    [SerializeField]
    private AnimatorController _animatorController;

    [SerializeField]
    private AnimationClip _idleAnim;

    [SerializeField]
    private AnimationClip _terrifiedAnim;

    [SerializeField]
    private AnimationClip _dieAnim;

    #endregion ___

    #region ___ SETTINGS ___
    protected override int defaultCapacity => 10;

    protected override int maxSize => 20;
    #endregion ___

    #region ___ DATA ___
    private Vector2Int _coord;

    public Vector2Int Coord => _coord;

    private DefenderState _state;

    public DefenderState State => _state;

    private AttackerGroup _combatTarget;

    public AttackerGroup CombatTarget => _combatTarget;
    #endregion ___


    public void Initialize(DefenderManager defenderManager, Vector2Int coord)
    {
        _defenderManager = defenderManager;
        _coord = coord;
        _state = DefenderState.Alive;
        _animatorController.PlayAnimation(_idleAnim);
    }


    #region ___ COMBAT ___

    public void SetCombatTarget(AttackerGroup combatTarget)
    {
        _combatTarget = combatTarget;
    }

    #endregion ___


    public void KillSelf()
    {
        _defenderManager.UnregisterDefender(this);
        _defenderManager.RoundManager.MapData.UpdateBlockType(MapBlockType.Empty, _coord);
        _state = DefenderState.None;
        _animatorController.PlayAnimation(_dieAnim, onFinished: () =>
        {
            ReleaseToPool();
        });

    }
}

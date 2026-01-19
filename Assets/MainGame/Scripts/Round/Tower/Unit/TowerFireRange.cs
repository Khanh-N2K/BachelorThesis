using N2K;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TowerFireRange : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private MeshRenderer _rangeSphere;

    [SerializeField]
    private GameObject _vfxObj;

    #endregion ___

    #region ___ DATA ___

    private HashSet<Attacker> _attackerSet = new();

    #endregion ___

    private void OnDisable()
    {
        foreach(var attacker in _attackerSet)
        {
            attacker.onDead -= OnAttackerDead;
        }
        _attackerSet.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(TagNameType.Attacker.ToString()))
        {
            return;
        }
        Attacker attacker = other.GetComponentInParent<Attacker>();
        if (attacker != null)
        {
            _attackerSet.Add(attacker);
            attacker.onDead += OnAttackerDead;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(TagNameType.Attacker.ToString()))
        {
            return;
        }
        Attacker attacker = other.GetComponentInParent<Attacker>();
        if (attacker != null)
        {
            attacker.onDead -= OnAttackerDead;
            _attackerSet.Remove(attacker);
        }
    }

    public Attacker GetClosestActiveAttacker()
    {
        float minDistance = float.MaxValue;
        Attacker chosenAttacker = null;
        foreach (var attacker in _attackerSet.ToList())
        {
            if (attacker == null || attacker.State == AttackerState.None)
            {
                _attackerSet.Remove(attacker);
                continue;
            }
            float distance = Vector3.Distance(transform.position, attacker.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                chosenAttacker = attacker;
            }
        }
        return chosenAttacker;
    }

    private void OnAttackerDead(Attacker attacker)
    {
        _attackerSet.Remove(attacker);
    }

    public void SetShowRange(bool show)
    {
        _rangeSphere.enabled = show;
        _vfxObj.gameObject.SetActive(show);
    }
}

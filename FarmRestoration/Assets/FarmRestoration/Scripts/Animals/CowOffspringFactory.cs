using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Reuses an adult's imported mesh, URP materials and animation for saved offspring.</summary>
    public static class CowOffspringFactory
    {
        public static CowAnimal Create(CowAnimal template, CowSaveData saved)
        {
            if (template == null || saved == null || !saved.offspring
                || string.IsNullOrEmpty(saved.id) || template.HomePen == null) return null;
            Vector3 position = new Vector3(saved.x, template.transform.position.y, saved.z);
            CowAnimal calf = Object.Instantiate(template, position, template.transform.rotation, template.transform.parent);
            calf.name = "DairyOffspring_" + saved.id;
            calf.Configure(saved.id, template.HomePen, template.Ground,
                calf.GetComponentInChildren<Animator>(), calf.GetComponent<LineRenderer>());
            calf.LifeCycle.SetAdultScale(template.LifeCycle.AdultScale);
            calf.Restore(saved);
            return calf;
        }
    }
}

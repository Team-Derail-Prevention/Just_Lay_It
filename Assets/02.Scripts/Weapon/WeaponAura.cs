using UnityEngine;

public class WeaponAura : SingletonBase<WeaponAura>
{
    protected override void Init()
    {
        base.Init();
    }

    public void ApplyGradeColor(GameObject auraRoot, string colorHex)
    {
        if (auraRoot == null || string.IsNullOrEmpty(colorHex))
        {
            return;
        }

        if (!ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            Debug.LogWarning($"[WeaponAura] 색상 파싱 실패: {colorHex}");
            return;
        }

        ParticleSystem[] particles = auraRoot.GetComponentsInChildren<ParticleSystem>(true);
        Debug.Log($"파티클 개수: {particles.Length}");
        for (int i = 0; i < particles.Length; i++)
        {
            var ps = particles[i];
            Debug.Log($"[WeaponAura] 대상: {ps.gameObject.name} (ID:{ps.gameObject.GetInstanceID()}), 경로: {GetPath(ps.transform)}");

            var main = ps.main;
            main.startColor = color;
            Debug.Log($"[WeaponAura] 세팅 직후 readback: mode={main.startColor.mode}, color={main.startColor.color}");

            var colorOverLifetime = ps.colorOverLifetime;
            if (colorOverLifetime.enabled)
            {
                Gradient originalGradient = colorOverLifetime.color.gradient;
                GradientAlphaKey[] originalAlphaKeys = originalGradient.alphaKeys;

                Gradient newGradient = new Gradient();
                newGradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                    originalAlphaKeys
                );
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(newGradient);
            }

            bool wasPlaying = ps.isPlaying;
            ps.Clear(true);
            if (wasPlaying)
            {
                ps.Play(true);
            }
        }

        Debug.Log($"[WeaponAura] {auraRoot.name} 색상 적용: {colorHex}");
    }

    private string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
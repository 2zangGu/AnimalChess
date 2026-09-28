using System;
using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// AttackEffects가 만드는 절차적 VFX 오브젝트에 붙는, 스스로 애니메이션하고 끝나면
    /// 스스로 Destroy(gameObject)하는 아주 작은 트윈 컴포넌트들의 모음.
    /// 전부 새 애셋 없이 Quad/LineRenderer + MaterialPropertyBlock만으로 움직인다.
    /// </summary>

    /// <summary>근접 공격: 공격자 위치에서 대상 쪽으로 살짝 돌진했다가 원위치로 돌아오며 사라진다.</summary>
    public class AttackLungeMotion : MonoBehaviour
    {
        private Vector3 _from;
        private Vector3 _to;
        private float _duration;
        private float _t;

        public void Setup(Vector3 from, Vector3 to, float duration)
        {
            _from = from;
            _to = to;
            _duration = Mathf.Max(0.05f, duration);
            transform.position = from;
        }

        private void Update()
        {
            _t += Time.deltaTime / _duration;
            if (_t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // 절반까지는 전진, 절반부터는 원위치로 - 짧은 왕복 돌진 모션.
            float phase = _t < 0.5f ? _t * 2f : 2f - _t * 2f;
            transform.position = Vector3.Lerp(_from, _to, phase);
            AttackEffectFacing.FaceCamera(transform);
        }
    }

    /// <summary>원거리 공격: 공격자에서 대상까지 날아가는 작은 발사체(총알/화염 등)를 표현한다.</summary>
    public class AttackProjectileMotion : MonoBehaviour
    {
        private Vector3 _from;
        private Vector3 _to;
        private float _duration;
        private float _t;
        private Action _onArrive;

        public void Setup(Vector3 from, Vector3 to, float duration, Action onArrive)
        {
            _from = from;
            _to = to;
            _duration = Mathf.Max(0.02f, duration);
            _onArrive = onArrive;
            transform.position = from;
        }

        private void Update()
        {
            _t += Time.deltaTime / _duration;
            if (_t >= 1f)
            {
                _onArrive?.Invoke();
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.Lerp(_from, _to, _t);
            AttackEffectFacing.FaceCamera(transform);
        }
    }

    /// <summary>히트 플래시: 시간이 지나며 커지면서 투명해지다가 사라진다.</summary>
    public class AttackFadeScaleMotion : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private float _duration;
        private float _endScaleMultiplier;
        private float _t;
        private Vector3 _baseScale;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;
        private Color _baseColor;

        public void Setup(float duration, float endScaleMultiplier)
        {
            _duration = Mathf.Max(0.05f, duration);
            _endScaleMultiplier = endScaleMultiplier;
            _baseScale = transform.localScale;
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _baseColor = _mpb.isEmpty ? Color.white : (Color)_mpb.GetVector(BaseColorId);
            }
        }

        private void Update()
        {
            _t += Time.deltaTime / _duration;
            if (_t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.localScale = Vector3.Lerp(_baseScale, _baseScale * _endScaleMultiplier, _t);

            if (_renderer != null)
            {
                var c = _baseColor;
                c.a = _baseColor.a * (1f - _t);
                _mpb.SetColor(BaseColorId, c);
                _renderer.SetPropertyBlock(_mpb);
            }

            AttackEffectFacing.FaceCamera(transform);
        }
    }

    /// <summary>타격 지점에서 사방으로 퍼지는 작은 파티클 조각들. 개수는 별(★) 단계에 따라 달라진다.</summary>
    public class AttackBurstParticleMotion : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Transform[] _particles;
        private Vector3[] _directions;
        private const float Speed = 2.2f;
        private const float Duration = 0.3f;
        private float _t;

        public void Setup(int count, float sizeScale, Color color)
        {
            count = Mathf.Max(1, count);
            _particles = new Transform[count];
            _directions = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i + UnityEngine.Random.Range(-10f, 10f);
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                _directions[i] = dir;

                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                go.name = "Particle";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one * 0.12f * sizeScale;

                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = AttackEffects.SharedMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                var mpb = new MaterialPropertyBlock();
                mpb.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(mpb);

                _particles[i] = go.transform;
            }
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (_t >= Duration)
            {
                Destroy(gameObject);
                return;
            }

            var cam = Camera.main;
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] == null) continue;
                _particles[i].localPosition = _directions[i] * (Speed * _t);
                if (cam != null)
                {
                    _particles[i].rotation = Quaternion.LookRotation(_particles[i].position - cam.transform.position, Vector3.up);
                }
            }
        }
    }

    /// <summary>
    /// 진화 단계(2성 이상)에서 보이는, 바깥으로 넓어지며 옅어지는 링.
    /// HexTile의 호버 테두리와 같은 LineRenderer 원 그리기 기법을 재사용한다.
    /// </summary>
    public class AttackRingExpandMotion : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private LineRenderer _line;
        private const float Duration = 0.35f;
        private const float StartRadius = 0.05f;
        private float _t;
        private float _maxRadius;
        private Color _color;

        public void Setup(float sizeScale, Color color)
        {
            _maxRadius = 0.55f * sizeScale;
            _color = color;

            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = 20;
            _line.widthMultiplier = 0.04f;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.sharedMaterial = AttackEffects.SharedMaterial;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, color);
            _line.SetPropertyBlock(mpb);

            SetRadius(StartRadius);
        }

        private void Update()
        {
            _t += Time.deltaTime / Duration;
            if (_t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            SetRadius(Mathf.Lerp(StartRadius, _maxRadius, _t));

            var c = _color;
            c.a = _color.a * (1f - _t);
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, c);
            _line.SetPropertyBlock(mpb);
        }

        private void SetRadius(float radius)
        {
            for (int i = 0; i < _line.positionCount; i++)
            {
                float angle = (360f / _line.positionCount) * i * Mathf.Deg2Rad;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }

    /// <summary>
    /// 3성 전용 추가 장식: 살짝 지연된 뒤에 재생되는 두 번째 확장 링(이중 펄스 느낌).
    /// </summary>
    public class AttackDelayedRingMotion : MonoBehaviour
    {
        private Vector3 _pos;
        private float _sizeScale;
        private Color _color;
        private float _delay;

        public void Setup(Vector3 pos, float sizeScale, Color color, float delay)
        {
            _pos = pos;
            _sizeScale = sizeScale;
            _color = color;
            _delay = delay;
        }

        private void Update()
        {
            _delay -= Time.deltaTime;
            if (_delay > 0f) return;

            var ringGO = new GameObject("FlourishRing");
            ringGO.transform.position = _pos;
            var ring = ringGO.AddComponent<AttackRingExpandMotion>();
            ring.Setup(_sizeScale * 1.3f, _color);

            Destroy(gameObject);
        }
    }

    /// <summary>모든 이펙트 조각이 공유하는 "항상 카메라를 바라보게" 헬퍼.</summary>
    internal static class AttackEffectFacing
    {
        public static void FaceCamera(Transform t)
        {
            var cam = Camera.main;
            if (cam == null) return;
            t.rotation = Quaternion.LookRotation(t.position - cam.transform.position, Vector3.up);
        }
    }
}

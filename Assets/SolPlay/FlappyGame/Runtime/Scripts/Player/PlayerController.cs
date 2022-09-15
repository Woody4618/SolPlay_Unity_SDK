using System.Collections;
using SolPlay.Deeplinks;
using UnityEngine;

[RequireComponent(typeof(PlayerAudio))]
[RequireComponent(typeof(PlayerInputs))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerInputs _input;
    [SerializeField] PlayerAudio _audio;
    [SerializeField] SpriteRenderer _spriteRenderer;
    [SerializeField] ParticleSystem _fireParticles;
    [SerializeField] GameObject _tradeGraphParticles;
    public PlayerParameters MovementParameters;

    Vector3 _velocity;
    Vector3 _rotation;

    private bool _isDead;
    private ParticleSystem.MinMaxCurve _initialParticleEmission;
    private Coroutine _fireDisableCoroutine;
    public Vector3 Velocity { get => _velocity; }

    public void SetSpriteFromNft(SolPlayNft nft)
    {
        var rect = new Rect(0, 0, nft.MetaplexData.nftImage.file.width, nft.MetaplexData.nftImage.file.height);
        _spriteRenderer.sprite = Sprite.Create(nft.MetaplexData.nftImage.file, rect, new Vector2(0.5f, 0.5f));
    }

    public void Die()
    {
        _isDead = true;
        _velocity = Vector3.zero;
        _audio.OnDie();
        CameraShake.Shake(0.1f, 0.2f);
        _tradeGraphParticles.gameObject.SetActive(false);
    }

    public void Reset()
    {
        transform.position = Vector3.zero;
        _isDead = false;
        transform.rotation = Quaternion.identity;
        _tradeGraphParticles.gameObject.SetActive(true);
    }

    private void Awake()
    {
        _velocity = new Vector3();
        _rotation = new Vector3();
        var fireParticlesEmission = _fireParticles.emission;
        _initialParticleEmission = fireParticlesEmission.rateOverDistance;
        fireParticlesEmission.rateOverDistance = 0;
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        ApplyGravity(in delta);
        TryApplyFallRotation(in delta);

        if(!_isDead)
        {
            MoveForward();
            ProcessInput();
        }

        transform.position += _velocity * delta;
        transform.rotation = Quaternion.Euler(_rotation);
    }

    private void ProcessInput() 
    {
        if(_input.TapUp())
        {
            Flap();
        }
    }

    public void OnHitGround()
    {
        _audio.OnHitGround();
        enabled = false;
    }

    public void Flap()
    {
        _velocity.y = MovementParameters.FlapSpeed;
        _rotation.z = MovementParameters.FlapRotation;
        _audio.OnFlap();

        if (!_tradeGraphParticles.gameObject.activeInHierarchy)
        {
            _tradeGraphParticles.gameObject.SetActive(true);   
        }

        var fireParticlesEmission = _fireParticles.emission;
        fireParticlesEmission.rateOverDistance = _initialParticleEmission;
        if (_fireDisableCoroutine != null)
        {
            StopCoroutine(_fireDisableCoroutine);
        }

        CameraShake.Shake(0.1f, 0.05f);
        _fireDisableCoroutine = StartCoroutine(SprayFire());
    }

    private IEnumerator SprayFire()
    {
        yield return new WaitForSeconds(0.2f);
        var fireParticlesEmission = _fireParticles.emission;
        fireParticlesEmission.rateOverDistance = 0;
    }

    private void MoveForward()
    {
        _velocity.x = MovementParameters.ForwardSpeed;
    }

    private void ApplyGravity(in float delta)
    {
        _velocity.y -= MovementParameters.Gravity * delta;
    }

    private void TryApplyFallRotation(in float delta)
    {
        if(_velocity.y <= 0)
        {
            _rotation.z -= delta * MovementParameters.FallingRotationSpeed;
            _rotation.z = Mathf.Clamp(
                                        _rotation.z, 
                                        MovementParameters.FallingRotationAngle,
                                        MovementParameters.FlapRotation
                                    );
        }
    }
}

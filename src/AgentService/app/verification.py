from .models import CadVerificationRequest, CadVerificationResponse


def verify(request: CadVerificationRequest) -> CadVerificationResponse:
    checks = []
    passed = True

    def check(name, ok, detail=''):
        nonlocal passed
        passed = passed and ok
        checks.append(f"{name}: {'PASS' if ok else 'FAIL'}" + (f' ({detail})' if detail else ''))

    check('rebuild', request.rebuild_ok)
    for name in ('width', 'height', 'thickness'):
        expected = getattr(request, f'expected_{name}_mm')
        measured = getattr(request, f'measured_{name}_mm')
        # Also scale tolerance down for very small dimensions.
        limit = min(0.05, expected * 0.005)
        check(name, abs(expected - measured) <= limit,
              f'expected {expected:g} mm, measured {measured:g} mm')
    expected_volume = request.expected_width_mm * request.expected_height_mm * request.expected_thickness_mm
    check('expected volume consistency', abs(expected_volume - request.expected_volume_mm3) <= expected_volume * 1e-9)
    check('volume', abs(request.measured_volume_mm3 - expected_volume) <= expected_volume * 0.005,
          f'expected {expected_volume:g} mm^3, measured {request.measured_volume_mm3:g} mm^3')
    check('native features', {'Mechra-Plate-Sketch', 'Mechra-Plate-Extrude'} <= set(request.feature_names))
    return CadVerificationResponse(passed=passed, checks=checks,
                                   message='Native Part verification passed.' if passed else 'Native Part verification failed.')

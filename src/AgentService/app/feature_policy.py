"""Classify feature snapshots; native body/sketch checks still run at Apply."""
BLANK_PART_TYPES = frozenset(name.casefold() for name in (
    'HistoryFolder', 'CommentsFolder', 'FavoriteFolder', 'SelectionSetFolder', 'SensorFolder',
    'DetailCabinet', 'MaterialFolder', 'SolidBodyFolder', 'SurfaceBodyFolder', 'RefPlane', 'OriginProfileFeature',
    'DocsFolder', 'EnvFolder', 'InkMarkupFolder', 'EqnFolder',
))


def equation_blocker(state):
    # Legacy/failed captures remain a native preflight decision. None never
    # authorizes a CAD mutation: the executor requires a successful empty read.
    if state is None:
        return None
    if state.count or state.disabled_count or state.linked_to_file:
        return ('Part có phương trình, biến toàn cục hoặc liên kết tệp phương trình. '
                'v0.2 chưa hỗ trợ tạo/sửa plate trong trạng thái này. '
                'Dùng Check Part để xem số lượng; hãy dùng Part riêng không có phương trình.')
    return None


def feature_allowed(feature, allow_plate=False):
    kind = feature.type_name or ''
    if kind.casefold() in BLANK_PART_TYPES:
        return True
    return allow_plate and (
        (feature.name == 'Mechra-Plate-Extrude' and kind in ('Extrusion', 'Boss', 'BaseBody'))
        or (feature.name == 'Mechra-Plate-Sketch' and kind == 'ProfileFeature'))


def first_blocking_feature(features, allow_plate=False):
    return next((feature for feature in features if not feature_allowed(feature, allow_plate)), None)

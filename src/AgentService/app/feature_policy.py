"""Classify feature snapshots; native body/sketch checks still run at Apply."""
BLANK_PART_TYPES = frozenset(name.casefold() for name in (
    'HistoryFolder', 'CommentsFolder', 'FavoriteFolder', 'SelectionSetFolder', 'SensorFolder',
    'DetailCabinet', 'MaterialFolder', 'SolidBodyFolder', 'SurfaceBodyFolder', 'RefPlane', 'OriginProfileFeature',
    'DocsFolder', 'EnvFolder',
))


def feature_allowed(feature, allow_plate=False):
    kind = feature.type_name or ''
    if kind.casefold() in BLANK_PART_TYPES:
        return True
    return allow_plate and (
        (feature.name == 'Mechra-Plate-Extrude' and kind in ('Extrusion', 'Boss', 'BaseBody'))
        or (feature.name == 'Mechra-Plate-Sketch' and kind == 'ProfileFeature'))


def first_blocking_feature(features, allow_plate=False):
    return next((feature for feature in features if not feature_allowed(feature, allow_plate)), None)

import sys
import os

import clr

def _add_reference(name, folder):
    try:
        clr.AddReference(name)
        return
    except Exception:
        pass
    clr.AddReferenceToFileAndPath(os.path.join(folder, name + '.dll'))

_add_reference('AlibreX', AS_AlibreProgramFolder)
_add_reference('AlibreScriptAddOn', AS_AlibreScriptFolder)

import AlibreX
from AlibreScript.API import *
from AlibreScript.API import Windows as _WindowsAPI
from AlibreScript.API import Global as _Global

if AS_Root is not None:
    _Global.Root = AS_Root

ScriptFileName = AS_ScriptPath
ScriptFolder = AS_ScriptFolder
Arguments = list(AS_Arguments)
SessionIdentifier = AS_SessionIdentifier
CurrentSession = AS_Session
AlibreRoot = AS_Root
ParentForm = AS_ParentForm

for _folder in (AS_ScriptFolder, AS_RuntimeFolder):
    if _folder and _folder not in sys.path:
        sys.path.insert(0, _folder)

_deferred = []
_library = None

def Windows():
    return _WindowsAPI(AS_SessionIdentifier, AS_ScriptPath, AS_ParentForm)

def Print(message):
    AS_Log(str(message))

def Tell(message, title, error=False):
    AS_Log('%s: %s' % (title, message.replace('\n', ' ')))
    _deferred.append((title, message, bool(error)))


def AS_TakeDeferred():
    queued = list(_deferred)
    del _deferred[:]
    return queued

def CollectionItems(collection):
    items = []
    if collection is None:
        return items
    try:
        for item in collection:
            items.append(item)
        if items:
            return items
    except Exception:
        items = []
    try:
        count = collection.Count
    except Exception:
        return items
    for first in (0, 1):
        probe = []
        try:
            for index in range(first, first + count):
                probe.append(collection.Item(index))
        except Exception:
            probe = []
        if len(probe) == count:
            return probe
    return items

def _folder_materials(folder, names):
    for material in CollectionItems(folder.Materials):
        names.add(material.Name)
    for child in CollectionItems(folder.SubFolders):
        _folder_materials(child, names)

def InstalledMaterials():
    global _library
    if _library is not None:
        return _library
    names = set()
    if AS_Root is not None:
        try:
            for library in CollectionItems(AS_Root.MaterialLibraries):
                for material in CollectionItems(library.Materials):
                    names.add(material.Name)
                for folder in CollectionItems(library.Folders):
                    _folder_materials(folder, names)
        except Exception, ex:
            AS_Log('Could not read the material libraries: %s' % ex)
    _library = names
    return names

def _alternatives(wanted, limit=8):
    names = InstalledMaterials()
    if not names:
        return []
    key = str(wanted).lower()
    hits = [name for name in sorted(names) if key in name.lower()]
    if not hits:
        for word in [w for w in key.replace('-', ' ').replace(',', ' ').split() if len(w) > 2]:
            for name in sorted(names):
                if word in name.lower() and name not in hits:
                    hits.append(name)
    return hits[:limit]

def _material_unavailable(wanted, error):
    AS_Log('Material "%s" could not be applied: %s' % (wanted, error))
    lines = ['Alibre Design has no material named "%s" in its material library.' % wanted,
             '',
             'The part keeps the material it already had. Its color was still changed to '
             'the color this script uses for "%s".' % wanted]
    choices = _alternatives(wanted)
    if choices:
        lines.append('')
        lines.append('Materials in your library with a similar name:')
        for choice in choices:
            lines.append('    ' + choice)
        lines.append('')
        lines.append('Run Set Color from Material again and pick one of those, or add "%s" to '
                     'your material library first.' % wanted)
    else:
        lines.append('')
        lines.append('Run Set Color from Material again and pick a material your library lists, '
                     'or add "%s" to your material library first.' % wanted)
    Tell('\n'.join(lines), 'Material not changed')



class GuardedPart(object):

    def __init__(self, part):
        object.__setattr__(self, '_part', part)

    def __getattr__(self, name):
        return getattr(object.__getattribute__(self, '_part'), name)

    def __setattr__(self, name, value):
        part = object.__getattribute__(self, '_part')
        if name == 'Material':
            try:
                part.Material = value
            except Exception, ex:
                _material_unavailable(value, ex)
            return
        setattr(part, name, value)

    def __repr__(self):
        return repr(object.__getattribute__(self, '_part'))

def CurrentPart():
    if not isinstance(AS_Session, AlibreX.IADPartSession):
        Tell('Open a part, then run Set Color from Material again.\n\n'
             'This add-on colors the part in the active window, so a part window has to be in '
             'front. Assembly and drawing windows cannot be colored this way.',
             'No part is open', True)
        sys.exit()
    return GuardedPart(Part(AS_Session))

def CurrentAssembly():
    if not isinstance(AS_Session, AlibreX.IADAssemblySession):
        Tell('Open an assembly, then run this add-on again.', 'No assembly is open', True)
        sys.exit()
    return Assembly(AS_Session)

def IsPartSession():
    return isinstance(AS_Session, AlibreX.IADPartSession)

def IsAssemblySession():
    return isinstance(AS_Session, AlibreX.IADAssemblySession)

def Parts():
    return list(_Global.Parts)

def Assemblies():
    return list(_Global.Assemblies)

def LogPath():
    return os.path.join(os.environ.get('LOCALAPPDATA', ''),
                        'Alibre AddOns', 'SetColorFromMaterial', 'addon.log')

# PSObject, models built on it, and AutomationNull

## Building a PSObject in C#

PowerShell 7 has a public `PSObject(int)` constructor, which sets the capacity for instance members (measured 2026-10-03 on PowerShell 7.6.6). So in C#, `new PSObject(42)` builds an empty custom object, not a wrapped 42, and so does a subclass constructor that calls `base(capacity)`. `PSObject.AsPSObject(42)` and `new PSObject((object)42)` wrap the value.

## Models built on PSObject

Measured 2026-10-25 on PowerShell 7.6.8 with throwaway `Add-Type` subclasses of `PSObject` that call `base(capacity)`, add note properties, and insert a type name, and with `SeriesObject` from the module's Debug build.

- **A custom object:** an instance's `ImmediateBaseObject` and `BaseObject` are the `PSCustomObject` placeholder. `-is [pscustomobject]`, `-is [psobject]`, and `-is` its own class are all `$true`.
- **Type names:** a new instance has only `System.Management.Automation.PSCustomObject` and `System.Object`, without its class's name. A `SeriesObject` built with `new` has only those two until its deserialization hook runs, and then has `MG.Sonarr.Next.Models.Series.SeriesObject`, `SeriesBase`, and `MG.Sonarr.Next.Models.SonarrObject` before them.
- **Members that PowerShell sees:** the instance's note properties, and `Equals`, `GetHashCode`, `GetType`, and `ToString` from the placeholder. The subclass's own C# properties and methods aren't among them: reading a C# property gives `$null`, and calling a C# method fails with `MethodNotFound` ("Method invocation failed because [<first type name>] does not contain a method named '<name>'."). So does an explicit interface implementation, even after a cast to the interface. Where a note property and a C# property share a name, PowerShell reads the note property, and a hashtable key named after a C# property adds a note property instead of setting it.
- **`.psobject`** is a member set, a `PSInternalMemberSet`, not the instance. It adapts the instance as a .NET object, so its C# members are there: `$model.psobject.Title` reads the C# property while `$model.Title` reads the note property, and `$model.psobject.OnDeserialized()` and an explicitly implemented interface method run through it.
- **In script,** `$model.GetType()` reports `System.Management.Automation.PSCustomObject`, `$model.ToString()` gives `''`, and `"$model"` gives the `@{Id=1; Title=...}` form. A .NET method that takes `[object]`, such as `PropertyInfo.GetValue`, receives the instance itself.
- **`Copy()`** returns a new instance of the same class, with copies of its type names and note properties: adding or changing a property, or inserting a type name, in the copy leaves the original as it was.
- **`PSSerializer`:** serializing an instance and reading it back gives a plain `PSObject` around a custom object, with the note properties, without the C# properties, and with `Deserialized.` before each type name.
- **`Select-Object`** with `-First`, `-Last`, `-Skip`, `-Index`, or `-Unique`, and without `-Property`, passes the same instance on and inserts `Selected.System.Management.Automation.PSCustomObject` at the front of its type names, once however often it runs. It does the same to a `[pscustomobject]`, while `Where-Object` and `Sort-Object` change nothing. With `-Property`, `Select-Object` builds a new `PSObject` instead, whose first type name is `Selected.System.Management.Automation.PSCustomObject`, and leaves the original as it was. A format view and type data for a type name further down the list still apply to the changed instance.

## PSObject's own members

Measured 2026-10-03 on PowerShell 7.6.6 from `Add-Type` C# that uses reflection, unless a fact gives another date.

- PowerShell 7.6.6's private members include the field `_immediateBaseObject` and the non-public property `ImmediateBaseObjectIsEmpty`, which reads a bit in `_flags`.
- `ImmediateBaseObject` is public, and so is the type `PSCustomObject`.
- **`ImmediateBaseObject is PSCustomObject` always equals the private empty flag.** Checked on 2026-10-03 for `[pscustomobject]`, `New-Object PSObject`, `new PSObject(<a PSCustomObject>)`, `AutomationNull.Value`, and ordinary values, and on 2026-10-06 for deserialized objects from `PSSerializer`, `Export-Clixml` and `Import-Clixml`, and `Start-Job` output: 166 PSObjects, no mismatch. The PowerShell 7.6.6 source agrees. Only `MshObject.cs` writes the flag, in the constructors through `CommonInitialization`, in `Copy`, and in `SetCoreOnDeserialization`, and every write keeps it equal to the test. The deserializer creates each `<Obj>` as `new PSObject()`.
- The public `PSObject.BaseObject` unwraps through a nested custom object to its `PSCustomObject` placeholder (measured 2026-10-06). Only the internal `PSObject.Base` stops at the custom object.
- `PSObject` overrides `Equals` (PowerShell 7.6.6 source): reference equality first, then `false` whenever `this.BaseObject` is the `PSCustomObject` placeholder, and otherwise `LanguagePrimitives.Equals(BaseObject, obj)`.

## AutomationNull

Measured 2026-10-03 on PowerShell 7.6.6, unless a fact gives another date.

- `AutomationNull.Value` is an empty `new PSObject()`. Its empty flag is true, and its immediate base object is a `PSCustomObject`.
- The parameter binder turns `(& {})` and `$x = & {}` into `null` before transformation attributes and cmdlet parameters see them, and `$null | cmd` gives `null`. `InvokeWithContext` drops it from its output. `ScriptBlock.InvokeReturnAsIs()` on an empty script does return it.
- Piping a variable that holds it sends no pipeline input at all (measured 2026-10-06). After `$x = & {}`, or a `$x = Get-ChildItem` that matches nothing, `$x | ForEach-Object { 'ran' }` outputs nothing. After `$x = $null`, the same pipeline runs once, with `$null` (measured 2026-10-22 on PowerShell 7.6.8).

## What it means for PoshSonarr

- A model built on `SonarrObject` is an empty custom object that holds its values as note properties, so `ImmediateBaseObject is PSCustomObject` is true for every model, and unwrapping one layer at a time through `ImmediateBaseObject` stops at a model and keeps it. `PSObject.BaseObject` unwraps a model to the placeholder, so don't use it to reach the model, and don't read `PSObject`'s private members, which a release can rename.
- Everything that a user, a format view, or type data should see on a model is a note property or type data, because PowerShell doesn't see a model's C# members. A C# property that copies a note property doesn't see a change PowerShell makes to the note property, nor the reverse, so code that has to honor a user's change reads the note property.
- A model gets the module's type names only from code that inserts them, such as its deserialization hook, so a model built with `new`, or by the binder from a hashtable, matches no format view or type data of the module until that code runs.
- Match a model by its class, or by any of its type names, never by the first one: `Select-Object -First` and the other switches that pass objects through put `Selected.System.Management.Automation.PSCustomObject` in front of a model's own names.
- `Copy()` gives an independent copy of the same class, so adding a property to the copy, such as to show it with more detail, leaves the original as it was.
- Build a wrapped value in C# with `PSObject.AsPSObject(x)` or `new PSObject((object)x)`, never `new PSObject(<int>)`, which builds an empty custom object.
- A script block that outputs nothing gives an empty collection through `InvokeWithContext`, not one element that holds `AutomationNull.Value`.

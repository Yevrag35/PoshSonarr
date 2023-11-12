using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.NonApi
{
    [Cmdlet(VerbsCommon.Get, "SonarrObjectProperty")]
    [Alias("Get-SonarrObjectProperties", "Get-SonarrProperties")]
    public sealed class GetSonarrObjectProperty : PoolableCmdlet
    {
        SortedSet<SonarrProperty> _set = null!;

        protected override int Capacity => 1;

        [Parameter(Position = 0, ValueFromPipeline = true)]
        public PSObject[] InputObject { get; set; } = Array.Empty<PSObject>();

        [Parameter]
        public SwitchParameter IncludeReadOnly { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            _set = this.GetPooledObject<SortedSet<SonarrProperty>>();
            this.GetReturnables()[0] = _set;
        }

        protected override void Process(IServiceProvider provider)
        {
            if (this.InputObject.Length <= 0)
            {
                return;
            }

            bool includeRo = this.IncludeReadOnly.ToBool();

            foreach (PSObject pso in this.InputObject)
            {
                foreach (PSPropertyInfo property in pso.Properties)
                {
                    if (includeRo || property.IsSettable)
                    {
                        _set.Add(new()
                        {
                            CurrentValue = property.Value,
                            IsReadOnly = !property.IsSettable,
                            Name = property.Name,
                            ObjectType = pso.GetType().GetPSTypeName(removeBrackets: true),
                            Type = property.TypeNameOfValue,
                        });
                    }
                }
            }
        }

        protected override void End(IServiceProvider provider)
        {
            if (_set.Count <= 0)
            {
                return;
            }

            this.WriteCollection(_set);
        }
    }
}


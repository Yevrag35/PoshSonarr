using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.NonApi
{
    [Cmdlet(VerbsCommon.Get, "SonarrObjectProperty")]
    [Alias("Get-SonarrObjectProperties", "Get-SonarrProperties")]
    public sealed class GetSonarrObjectProperty : SonarrCmdletBase
    {
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public PSObject[] InputObject { get; set; } = Array.Empty<PSObject>();

        [Parameter]
        public SwitchParameter IncludeReadOnly { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            if (this.InputObject.Length <= 0)
            {
                return;
            }

            bool includeRo = this.IncludeReadOnly.ToBool();

            var set = provider.GetRequiredService<SortedSet<SonarrProperty>>();
            foreach (var pso in this.InputObject)
            {
                foreach (var property in pso.Properties)
                {
                    if (includeRo || property.IsSettable)
                    {
                        set.Add(new()
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
            var set = provider.GetRequiredService<SortedSet<SonarrProperty>>();
            if (set.Count <= 0)
            {
                return;
            }

            this.WriteCollection(set);
        }
    }
}


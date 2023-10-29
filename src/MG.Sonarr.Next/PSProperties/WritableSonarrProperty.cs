using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Models;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class WritableSonarrProperty<T> : WritableProperty<T> where T : SonarrObject
    {
        public T? SonarrObject { get; set; }
        public override string TypeNameOfValue => GetTypeNameOfValueOrDefault(this.SonarrObject);
        protected override T? ValueAsT
        {
            get => this.SonarrObject;
            set => this.SonarrObject = value;
        }

        public WritableSonarrProperty(string propertyName)
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            this.SetMemberName(propertyName);
        }
        public WritableSonarrProperty(string propertyName, T? value)
            : this(propertyName)
        {
            this.SonarrObject = value;
        }

        protected override PSPropertyInfo CopyIntoNew(string name)
        {
            if (this.SonarrObject?.Copy() is not T tObj)
            {
                tObj = this.SonarrObject!;
            }

            return new WritableSonarrProperty<T>(name, tObj);
        }

        protected override T? ConvertFromObject(object? value)
        {
            return value is T tObj ? tObj : this.ThrowNotType<T>();
        }

        private static string GetTypeNameOfValueOrDefault(T? value)
        {
            if (value is null)
            {
                return typeof(T).GetTypeName();
            }

            return value.TypeNames[0];
        }

        protected override ReadOnlyProperty<T> CopyToReadOnly()
        {
            return new ReadOnlyPSObjectProperty<T>(this.Name, this.SonarrObject!);
        }
    }
}


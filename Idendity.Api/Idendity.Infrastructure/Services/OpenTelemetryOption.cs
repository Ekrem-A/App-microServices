using System;
using System.Collections.Generic;
using System.Text;

namespace KubernetesLessons.ServiceDefaults
{
    public class OpenTelemetryOption
    {
        public string ServiceName { get; set; } = null!;

        public string ServiceVersion { get; set; } = null!;

        public string ActivitySourceName { get; set; } = null!;
    }
}

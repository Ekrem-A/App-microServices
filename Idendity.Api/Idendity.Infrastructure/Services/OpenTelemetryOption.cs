using System;
using System.Collections.Generic;
using System.Text;

namespace Idendity.Infrastructure.Services
{
    public class OpenTelemetryOption
    {
        public string ServiceName { get; set; } = null!;

        public string ServiceVersion { get; set; } = null!;

        public string ActivitySourceName { get; set; } = null!;
    }
}

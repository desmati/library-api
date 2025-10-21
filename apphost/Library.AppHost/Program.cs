// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("library-postgres")
	.WithPgAdmin()
	.AddDatabase("libdb")
	;

var seq = builder.AddSeq("seq")
	.WithHttpEndpoint(port: 5341, targetPort: 80, name: "seqhttp")
	; // TODO: Secure Seq with API key enforcement.

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "latest")
	.WithHttpEndpoint(port: 16686, targetPort: 16686, name: "jaeger-ui")
	.WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc", scheme: "http")
	.WithEnvironment("COLLECTOR_OTLP_ENABLED", "true")
	;

var migrations = builder.AddProject<Projects.Library_MigrationService>("migrations")
	.WithReference(postgres)
	.WaitFor(postgres)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaeger.GetEndpoint("otlp-grpc"))
	.WithEnvironment("OpenTelemetry__OtlpEndpoint", jaeger.GetEndpoint("otlp-grpc"))
	.WithReference(seq)
	.WithEnvironment("SEQ_URL", seq.GetEndpoint("seqhttp"))
	.WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("seqhttp"))
	;

var grpcService = builder.AddProject<Projects.Library_Grpc>("library-grpc")
	.WithReference(migrations)
	.WaitForCompletion(migrations)
	.WithReference(postgres)
	.WaitFor(postgres)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaeger.GetEndpoint("otlp-grpc"))
	.WithEnvironment("OpenTelemetry__OtlpEndpoint", jaeger.GetEndpoint("otlp-grpc"))
	.WithReference(seq)
	.WithEnvironment("SEQ_URL", seq.GetEndpoint("seqhttp"))
	.WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("seqhttp"))
;

builder.AddProject<Projects.Library_Api>("library-api")
	.WithReference(migrations)
	.WaitForCompletion(migrations)
	.WithReference(grpcService)
	.WaitFor(grpcService)
	.WithEnvironment("GrpcServices__LibraryGrpc", grpcService.GetEndpoint("http"))
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaeger.GetEndpoint("otlp-grpc"))
	.WithEnvironment("OpenTelemetry__OtlpEndpoint", jaeger.GetEndpoint("otlp-grpc"))
	.WithReference(seq)
	.WithEnvironment("SEQ_URL", seq.GetEndpoint("seqhttp"))
	.WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("seqhttp"))
	;

builder.Build().Run();

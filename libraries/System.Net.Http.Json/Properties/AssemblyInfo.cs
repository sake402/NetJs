using NetJs;
using System.Diagnostics.CodeAnalysis;

//Prevents models(T) fetched from Http from being constructor and property trimmed

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpClient,System.String,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpClient,System.String,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpClient,System.String,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpClient,System.Uri,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpClient,System.Uri,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpClient,System.Uri,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync(System.Net.Http.HttpClient,System.String,System.Type,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync(System.Net.Http.HttpClient,System.String,System.Type,System.Text.Json.Serialization.JsonSerializerContext,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync(System.Net.Http.HttpClient,System.String,System.Type,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync(System.Net.Http.HttpClient,System.Uri,System.Type,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync(System.Net.Http.HttpClient,System.Uri,System.Type,System.Text.Json.Serialization.JsonSerializerContext,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync(System.Net.Http.HttpClient,System.Uri,System.Type,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,TValue,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,TValue,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync(System.Net.Http.HttpClient,System.String,System.Type,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync(System.Net.Http.HttpClient,System.String,System.Type,System.Text.Json.Serialization.JsonSerializerContext,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync(System.Net.Http.HttpClient,System.String,System.Type,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync(System.Net.Http.HttpClient,System.Uri,System.Type,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync(System.Net.Http.HttpClient,System.Uri,System.Type,System.Text.Json.Serialization.JsonSerializerContext,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync(System.Net.Http.HttpClient,System.Uri,System.Type,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.String,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpClientJsonExtensions.DeleteFromJsonAsync<TValue>(System.Net.Http.HttpClient,System.Uri,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpContent,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpContent,System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsAsyncEnumerable<TValue>(System.Net.Http.HttpContent,System.Threading.CancellationToken).TValue",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync(System.Net.Http.HttpContent,System.Type,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync(System.Net.Http.HttpContent,System.Type,System.Text.Json.Serialization.JsonSerializerContext,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]


[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync(System.Net.Http.HttpContent,System.Type,System.Threading.CancellationToken).type",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(System.Net.Http.HttpContent,System.Text.Json.JsonSerializerOptions,System.Threading.CancellationToken).T",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(System.Net.Http.HttpContent,System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>,System.Threading.CancellationToken).T",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(System.Net.Http.HttpContent,System.Threading.CancellationToken).T",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

[assembly: Attached(
    "System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(System.Net.Http.HttpContent,System.Threading.CancellationToken).T",
    typeof(DynamicallyAccessedMembersAttribute),
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

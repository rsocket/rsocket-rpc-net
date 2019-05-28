/*
 *
 * Copyright (c) 2017-present, Netifi Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

#include <cctype>
#include <map>
#include <sstream>
#include <vector>

#include "csharp_generator.h"
#include "csharp_generator_helpers.h"
#include "rsocket/options.pb.h"
#include <google/protobuf/io/printer.h>
#include <google/protobuf/io/zero_copy_stream_impl_lite.h>
#include <google/protobuf/compiler/csharp/csharp_names.h>

using google::protobuf::compiler::csharp::GetClassName;
using google::protobuf::compiler::csharp::GetFileNamespace;
using google::protobuf::compiler::csharp::GetReflectionClassName;
using google::protobuf::Descriptor;
using google::protobuf::FileDescriptor;
using google::protobuf::MethodDescriptor;
using google::protobuf::ServiceDescriptor;
using google::protobuf::io::Printer;
using google::protobuf::io::StringOutputStream;
using io::rsocket::rpc::RSocketMethodOptions;
using std::map;
using std::vector;

namespace rsocket_rpc_csharp_generator {
namespace {

bool GenerateDocCommentBodyImpl(google::protobuf::io::Printer* printer,
                                google::protobuf::SourceLocation location) {
  string comments = location.leading_comments.empty()
                              ? location.trailing_comments
                              : location.leading_comments;
  if (comments.empty()) {
    return false;
  }
  // XML escaping... no need for apostrophes etc as the whole text is going to
  // be a child
  // node of a summary element, not part of an attribute.
  comments = StringReplace(comments, "&", "&amp;", true);
  comments = StringReplace(comments, "<", "&lt;", true);

  std::vector<string> lines;
  Split(comments, '\n', &lines);
  // TODO: We really should work out which part to put in the summary and which
  // to put in the remarks...
  // but that needs to be part of a bigger effort to understand the markdown
  // better anyway.
  printer->Print("/// <summary>\n");
  bool last_was_empty = false;
  // We squash multiple blank lines down to one, and remove any trailing blank
  // lines. We need
  // to preserve the blank lines themselves, as this is relevant in the
  // markdown.
  // Note that we can't remove leading or trailing whitespace as *that's*
  // relevant in markdown too.
  // (We don't skip "just whitespace" lines, either.)
  for (std::vector<string>::iterator it = lines.begin();
       it != lines.end(); ++it) {
    string line = *it;
    if (line.empty()) {
      last_was_empty = true;
    } else {
      if (last_was_empty) {
        printer->Print("///\n");
      }
      last_was_empty = false;
      printer->Print("///$line$\n", "line", *it);
    }
  }
  printer->Print("/// </summary>\n");
  return true;
}

template <typename DescriptorType>
bool GenerateDocCommentBody(google::protobuf::io::Printer* printer,
                            const DescriptorType* descriptor) {
  google::protobuf::SourceLocation location;
  if (!descriptor->GetSourceLocation(&location)) {
    return false;
  }
  return GenerateDocCommentBodyImpl(printer, location);
}

void GenerateDocCommentMethod(google::protobuf::io::Printer* printer,
    const MethodDescriptor* method) {
  if (GenerateDocCommentBody(printer, method)) {
    if (method->client_streaming()) {
      printer->Print(
          "/// <param name=\"requestStream\">Used for reading requests from "
          "the client.</param>\n");
    } else {
      printer->Print(
          "/// <param name=\"request\">The request received from the "
          "client.</param>\n");
    }
    if (method->server_streaming()) {
      printer->Print(
          "/// <param name=\"responseStream\">Used for sending responses back "
          "to the client.</param>\n");
    }
    printer->Print(
        "/// <param name=\"context\">The context of the server-side call "
        "handler being invoked.</param>\n");
    if (method->server_streaming()) {
      printer->Print(
          "/// <returns>A task indicating completion of the "
          "handler.</returns>\n");
    } else {
      printer->Print(
          "/// <returns>The response to send back to the client (wrapped by a "
          "task).</returns>\n");
    }
  }
}

inline string CapitalizeFirstLetter(string s) {
  if (s.empty()) {
    return s;
  }
  s[0] = ::toupper(s[0]);
  return s;
}

std::string GetServiceClassName(const ServiceDescriptor* service) {
  return service->name();
}

std::string GetInterfaceName(const ServiceDescriptor* service) {
  return "I" + service->name();
}

std::string GetClientClassName(const ServiceDescriptor* service) {
  return service->name() + "Client";
}

std::string GetServerClassName(const ServiceDescriptor* service) {
  return service->name() + "Server";
}

std::string GetServiceFieldName() { return "__Service"; }

std::string GetMethodFieldName(const MethodDescriptor* method) {
  return "__Method_" + CapitalizeFirstLetter(method->name());
}

// Gets vector of all messages used as input or output types.
std::vector<const Descriptor*> GetUsedMessages(
    const ServiceDescriptor* service) {
  std::set<const Descriptor*> descriptor_set;
  std::vector<const Descriptor*>
      result;  // vector is to maintain stable ordering
  for (int i = 0; i < service->method_count(); i++) {
    const MethodDescriptor* method = service->method(i);
    if (descriptor_set.find(method->input_type()) == descriptor_set.end()) {
      descriptor_set.insert(method->input_type());
      result.push_back(method->input_type());
    }
    if (descriptor_set.find(method->output_type()) == descriptor_set.end()) {
      descriptor_set.insert(method->output_type());
      result.push_back(method->output_type());
    }
  }
  return result;
}

void GenerateStaticMethodField(Printer* out, const MethodDescriptor* method) {
  out->Print("public const string $methodfield$ = \"$methodname$\";\n",
             "methodfield", GetMethodFieldName(method),
             "methodname", CapitalizeFirstLetter(method->name()));
}

void GenerateServiceDescriptorProperty(Printer* out,
                                       const ServiceDescriptor* service) {
  std::ostringstream index;
  index << service->index();
  out->Print("/// <summary>Service descriptor</summary>\n");
  out->Print(
      "public static global::Google.Protobuf.Reflection.ServiceDescriptor "
      "Descriptor\n");
  out->Print("{\n");
  out->Print("  get { return $umbrella$.Descriptor.Services[$index$]; }\n",
             "umbrella", GetReflectionClassName(service->file()), "index",
             index.str());
  out->Print("}\n");
  out->Print("\n");
}

void GenerateInterface(Printer* out, const ServiceDescriptor* service) {
  out->Print("/// <summary>Interface for $servicename$</summary>\n", "servicename",
             GetServiceClassName(service));
  out->Print("public interface $interfacename$\n", "interfacename",
             GetInterfaceName(service));
  out->Print("{\n");
  out->Indent();

  // RPC methods
  for (int i = 0; i < service->method_count(); ++i) {
    const MethodDescriptor* method = service->method(i);
    const RSocketMethodOptions options = method->options().GetExtension(io::rsocket::rpc::options);
    bool client_streaming = method->client_streaming();
    bool server_streaming = method->server_streaming();

    // Method signature
    GenerateDocCommentMethod(out, method);

    if (server_streaming) {
      out->Print("IAsyncEnumerable<$output_type$> $method_name$",
          "output_type", GetClassName(method->output_type()),
          "method_name", CapitalizeFirstLetter(method->name()));
    } else if (client_streaming) {
      out->Print("Task<$output_type$> $method_name$",
          "output_type", GetClassName(method->output_type()),
          "method_name", CapitalizeFirstLetter(method->name()));
    } else {
      if (options.fire_and_forget()) {
        out->Print("Task $method_name$",
            "method_name", CapitalizeFirstLetter(method->name()));
      } else {
        out->Print("Task<$output_type$> $method_name$",
            "output_type", GetClassName(method->output_type()),
            "method_name", CapitalizeFirstLetter(method->name()));
      }
    }

    if (client_streaming) {
      // Bidirectional streaming or client streaming
      out->Print("(IAsyncEnumerable<$input_type$> messages, ReadOnlySequence<byte> metadata);\n",
          "input_type", GetClassName(method->input_type()));
    } else {
      // Server streaming or simple RPC
      out->Print("($input_type$ message, ReadOnlySequence<byte> metadata);\n",
          "input_type", GetClassName(method->input_type()));
    }
  }

  out->Outdent();
  out->Print("}\n");
  out->Print("\n");
}

void GenerateClientClass(Printer* out, const ServiceDescriptor* service) {
  out->Print("/// <summary>Client for $servicename$</summary>\n", "servicename",
             GetServiceClassName(service));
  out->Print("public class $clientname$ : RSocketService, $interfacename$\n",
      "clientname", GetClientClassName(service), "interfacename", GetInterfaceName(service));
  out->Print("{\n");
  out->Indent();

  out->Print("public $clientname$(RSocketClient client) : base(client) { }\n",
             "clientname", GetClientClassName(service));
  out->Print("\n");

  // RPC methods
  for (int i = 0; i < service->method_count(); ++i) {
    const MethodDescriptor* method = service->method(i);
    const RSocketMethodOptions options = method->options().GetExtension(io::rsocket::rpc::options);
    bool client_streaming = method->client_streaming();
    bool server_streaming = method->server_streaming();

    if (server_streaming) {
      out->Print("public IAsyncEnumerable<$output_type$> $method_name$",
                 "output_type", GetClassName(method->output_type()),
                 "method_name", CapitalizeFirstLetter(method->name()));
    } else if (client_streaming) {
      out->Print("public Task<$output_type$> $method_name$",
                 "output_type", GetClassName(method->output_type()),
                 "method_name", CapitalizeFirstLetter(method->name()));
    } else {
      if (options.fire_and_forget()) {
        out->Print("public Task $method_name$",
                   "method_name", CapitalizeFirstLetter(method->name()));
      } else {
        out->Print("public Task<$output_type$> $method_name$",
                   "output_type", GetClassName(method->output_type()),
                   "method_name", CapitalizeFirstLetter(method->name()));
      }
    }

    if (client_streaming) {
      // Bidirectional streaming or client streaming
      out->Print("(IAsyncEnumerable<$input_type$> messages, ReadOnlySequence<byte> metadata) =>\n",
                 "input_type", GetClassName(method->input_type()));
    } else {
      // Server streaming or simple RPC
      out->Print("($input_type$ message, ReadOnlySequence<byte> metadata) =>\n",
                 "input_type", GetClassName(method->input_type()));
    }

    out->Indent();
    if (client_streaming) {
      if (server_streaming) {
        out->Print("__RequestChannel(messages, $intransform$, $outtransform$, metadata, service: $servicefield$, method: $methodfield$);\n",
            "intransform", "Google.Protobuf.MessageExtensions.ToByteArray",
            "outtransform", GetClassName(method->output_type()) + ".Parser.ParseFrom",
            "servicefield", GetServiceFieldName(),
            "methodfield", GetMethodFieldName(method));
      } else {
        out->Print("__RequestChannel(messages, $intransform$, $outtransform$, metadata, service: $servicefield$, method: $methodfield$).SingleAsync().AsTask();\n",
            "intransform", "Google.Protobuf.MessageExtensions.ToByteArray",
            "outtransform", GetClassName(method->output_type()) + ".Parser.ParseFrom",
            "servicefield", GetServiceFieldName(),
            "methodfield", GetMethodFieldName(method));
      }
    } else if (server_streaming) {
      out->Print("__RequestStream(message, $intransform$, $outtransform$, metadata, service: $servicefield$, method: $methodfield$);\n",
          "intransform", "Google.Protobuf.MessageExtensions.ToByteArray",
          "outtransform", GetClassName(method->output_type()) + ".Parser.ParseFrom",
          "servicefield", GetServiceFieldName(),
          "methodfield", GetMethodFieldName(method));
    } else {
      if (options.fire_and_forget()) {
        out->Print("__RequestFireAndForget(message, $intransform$, metadata, service: $servicefield$, method: $methodfield$);\n",
            "intransform", "Google.Protobuf.MessageExtensions.ToByteArray",
            "servicefield", GetServiceFieldName(),
            "methodfield", GetMethodFieldName(method));
      } else {
        out->Print("__RequestResponse(message, $intransform$, $outtransform$, metadata, service: $servicefield$, method: $methodfield$);\n",
            "intransform", "Google.Protobuf.MessageExtensions.ToByteArray",
            "outtransform", GetClassName(method->output_type()) + ".Parser.ParseFrom",
            "servicefield", GetServiceFieldName(),
            "methodfield", GetMethodFieldName(method));
      }
    }

    out->Outdent();
    out->Print("\n");
  }

  out->Outdent();
  out->Print("}\n");
  out->Print("\n");
}

void GenerateServerClass(Printer* out, const ServiceDescriptor* service) {
  out->Print(
      "/// <summary>Base class for server-side implementations of "
      "$servicename$</summary>\n",
      "servicename", GetServiceClassName(service));
  out->Print("public abstract class $servername$ : IRSocketService, $interfacename$\n",
      "servername", GetServerClassName(service), "interfacename", GetInterfaceName(service));
  out->Print("{\n");
  out->Indent();

  out->Print("string IRSocketService.ServiceName => $servicefield$;\n",
             "servicefield", GetServiceFieldName());
  out->Print("IAsyncEnumerable<ReadOnlySequence<byte>> IRSocketService.Dispatch(ReadOnlySequence<byte> data, string method, ReadOnlySequence<byte> tracing, ReadOnlySequence<byte> metadata, IAsyncEnumerable<ReadOnlySequence<byte>> messages)\n");
  out->Indent();
  out->Print("=> from result in Dispatch(this, data, method, tracing, metadata, from message in messages select message.ToArray()) select new ReadOnlySequence<byte>(result);\n");
  out->Outdent();
  out->Print("\n");

  out->Print("public $servername$(RSocket.RSocket socket)\n",
             "servername", GetServerClassName(service));
  out->Print("{\n");
  out->Indent();
  out->Print("RSocketService.Register(socket, this);\n");
  out->Outdent();
  out->Print("}\n");
  out->Print("\n");

  // RPC methods
  for (int i = 0; i < service->method_count(); ++i) {
    const MethodDescriptor *method = service->method(i);
    const RSocketMethodOptions options = method->options().GetExtension(io::rsocket::rpc::options);
    bool client_streaming = method->client_streaming();
    bool server_streaming = method->server_streaming();

    if (server_streaming) {
      out->Print("public abstract IAsyncEnumerable<$output_type$> $method_name$",
                 "output_type", GetClassName(method->output_type()),
                 "method_name", CapitalizeFirstLetter(method->name()));
    } else if (client_streaming) {
      out->Print("public abstract Task<$output_type$> $method_name$",
                 "output_type", GetClassName(method->output_type()),
                 "method_name", CapitalizeFirstLetter(method->name()));
    } else {
      if (options.fire_and_forget()) {
        out->Print("public abstract Task $method_name$",
                   "method_name", CapitalizeFirstLetter(method->name()));
      } else {
        out->Print("public abstract Task<$output_type$> $method_name$",
                   "output_type", GetClassName(method->output_type()),
                   "method_name", CapitalizeFirstLetter(method->name()));
      }
    }

    if (client_streaming) {
      // Bidirectional streaming or client streaming
      out->Print("(IAsyncEnumerable<$input_type$> messages, ReadOnlySequence<byte> metadata);\n",
                 "input_type", GetClassName(method->input_type()));
    } else {
      // Server streaming or simple RPC
      out->Print("($input_type$ message, ReadOnlySequence<byte> metadata);\n",
                 "input_type", GetClassName(method->input_type()));
    }
  }

  out->Print("static IAsyncEnumerable<byte[]> Dispatch($interfacename$ service, ReadOnlySequence<byte> data, string method, in ReadOnlySequence<byte> tracing, in ReadOnlySequence<byte> metadata, IAsyncEnumerable<byte[]> messages)\n",
             "interfacename", GetInterfaceName(service));
  out->Print("{\n");
  out->Indent();
  out->Print("switch (method)\n");
  out->Print("{\n");
  out->Indent();

  // RPC methods
  for (int i = 0; i < service->method_count(); ++i) {
    const MethodDescriptor *method = service->method(i);
    const RSocketMethodOptions options = method->options().GetExtension(io::rsocket::rpc::options);
    bool client_streaming = method->client_streaming();
    bool server_streaming = method->server_streaming();

    if (client_streaming) {
      if (server_streaming) {
        out->Print("case $methodfield$: return from result in service.$method_name$(from message in messages select $input_type$.Parser.ParseFrom(data.ToArray()), metadata) select Google.Protobuf.MessageExtensions.ToByteArray(result);\n",
            "methodfield", GetMethodFieldName(method),
            "method_name", CapitalizeFirstLetter(method->name()),
            "input_type", GetClassName(method->input_type()));
      } else {
        out->Print("case $methodfield$: return from result in service.$method_name$(from message in messages select $input_type$.Parser.ParseFrom(data.ToArray()), metadata).ToAsyncEnumerable() select Google.Protobuf.MessageExtensions.ToByteArray(result);\n",
            "methodfield", GetMethodFieldName(method),
            "method_name", CapitalizeFirstLetter(method->name()),
            "input_type", GetClassName(method->input_type()));
      }
    } else if (server_streaming) {
      out->Print("case $methodfield$: return from result in service.$method_name$($input_type$.Parser.ParseFrom(data.ToArray()), metadata) select Google.Protobuf.MessageExtensions.ToByteArray(result);\n",
          "methodfield", GetMethodFieldName(method),
          "method_name", CapitalizeFirstLetter(method->name()),
          "input_type", GetClassName(method->input_type()));
    } else {
      if (options.fire_and_forget()) {
        out->Print("case $methodfield$: service.$method_name$($input_type$.Parser.ParseFrom(data.ToArray()), metadata); return AsyncEnumerable.Empty<byte[]>();\n",
          "methodfield", GetMethodFieldName(method),
          "method_name", CapitalizeFirstLetter(method->name()),
          "input_type", GetClassName(method->input_type()));
      } else {
        out->Print("case $methodfield$: return from result in service.$method_name$($input_type$.Parser.ParseFrom(data.ToArray()), metadata).ToAsyncEnumerable() select Google.Protobuf.MessageExtensions.ToByteArray(result);\n",
          "methodfield", GetMethodFieldName(method),
          "method_name", CapitalizeFirstLetter(method->name()),
          "input_type", GetClassName(method->input_type()));
      }
    }
  }

  out->Print("default: throw new InvalidOperationException(\"Unknown method: \" + method);\n",
      "servicefield", GetServiceFieldName());

  out->Outdent();
  out->Print("}\n");
  out->Outdent();
  out->Print("}\n");
  out->Outdent();
  out->Print("}\n");
  out->Print("\n");
}

void GenerateService(Printer* out, const ServiceDescriptor* service,
                     bool generate_client, bool generate_server) {
  GenerateDocCommentBody(out, service);
  out->Print("public static class $classname$\n",
             "classname", GetServiceClassName(service));
  out->Print("{\n");
  out->Indent();
  out->Print("public const string $servicefield$ = \"$servicename$\";\n",
             "servicefield", GetServiceFieldName(), "servicename",
             service->full_name());
  for (int i = 0; i < service->method_count(); i++) {
    GenerateStaticMethodField(out, service->method(i));
  }
  out->Print("\n");

  GenerateServiceDescriptorProperty(out, service);

  GenerateInterface(out, service);

  if (generate_client) {
    GenerateClientClass(out, service);
  }
  if (generate_server) {
    GenerateServerClass(out, service);
  }

  out->Outdent();
  out->Print("}\n");
}

}  // anonymous namespace

string GetServices(const FileDescriptor* file, bool generate_client, bool generate_server) {
  string output;
  {
    // Scope the output stream so it closes and finalizes output to the string.

    StringOutputStream output_stream(&output);
    Printer out(&output_stream, '$');

    // Don't write out any output if there no services, to avoid empty service
    // files being generated for proto files that don't declare any.
    if (file->service_count() == 0) {
      return output;
    }

    // Write out a file header.
    out.Print("// <auto-generated>\n");
    out.Print(
        "//     Generated by the protocol buffer compiler.  DO NOT EDIT!\n");
    out.Print("//     source: $filename$\n", "filename", file->name());
    out.Print("// </auto-generated>\n");

    // use C++ style as there are no file-level XML comments in .NET
    string leading_comments = GetCsharpComments(file, true);
    if (!leading_comments.empty()) {
      out.Print("// Original file comments:\n");
      out.PrintRaw(leading_comments.c_str());
    }

    out.Print("#pragma warning disable 0414, 1591\n");

    out.Print("#region Designer generated code\n");
    out.Print("\n");
    out.Print(
        "using System;\n"
        "using System.Buffers;\n"
        "using System.Collections.Generic;\n"
        "using System.Threading.Tasks;\n"
        "using RSocket;\n"
        "using RSocket.RPC;\n"
        "using System.Linq;\n");
    out.Print("\n");

    string file_namespace = GetFileNamespace(file);
    if (file_namespace != "") {
      out.Print("namespace $namespace$ {\n", "namespace", file_namespace);
      out.Indent();
    }
    for (int i = 0; i < file->service_count(); i++) {
      GenerateService(&out, file->service(i), generate_client, generate_server);
    }
    if (file_namespace != "") {
      out.Outdent();
      out.Print("}\n");
    }
    out.Print("#endregion\n");
  }
  return output;
}

}  // namespace rsocket_rpc_csharp_generator

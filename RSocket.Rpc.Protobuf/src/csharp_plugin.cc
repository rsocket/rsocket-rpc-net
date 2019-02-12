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

// Generates C# RSocket RPC service interface out of Protobuf IDL.

#include <memory>

#include "csharp_generator.h"
#include "csharp_generator_helpers.h"
#include <google/protobuf/compiler/code_generator.h>
#include <google/protobuf/compiler/plugin.h>
#include <google/protobuf/descriptor.h>
#include <google/protobuf/io/zero_copy_stream.h>
#include <iostream>

class CSharpRSocketGenerator : public google::protobuf::compiler::CodeGenerator {
 public:
  CSharpRSocketGenerator() {}
  ~CSharpRSocketGenerator() {}

  bool Generate(const google::protobuf::FileDescriptor* file,
                const string& parameter,
                google::protobuf::compiler::GeneratorContext* context,
                string* error) const {
    std::vector<std::pair<string, string> > options;
    google::protobuf::compiler::ParseGeneratorParameter(parameter, &options);

    bool generate_client = true;
    bool generate_server = true;
    for (size_t i = 0; i < options.size(); i++) {
      if (options[i].first == "no_client") {
        generate_client = false;
      } else if (options[i].first == "no_server") {
        generate_server = false;
      } else {
        *error = "Unknown generator option: " + options[i].first;
        return false;
      }
    }

    string code = rsocket_rpc_csharp_generator::GetServices(
        file, generate_client, generate_server);
    if (code.size() == 0) {
      return true;  // don't generate a file if there are no services
    }

    // Get output file name.
    string file_name;
    if (!rsocket_rpc_csharp_generator::ServicesFilename(file, &file_name)) {
      return false;
    }
    std::unique_ptr<google::protobuf::io::ZeroCopyOutputStream> output(
        context->Open(file_name));
    google::protobuf::io::CodedOutputStream coded_out(output.get());
    coded_out.WriteRaw(code.data(), code.size());
    return true;
  }
};

int main(int argc, char* argv[]) {
  CSharpRSocketGenerator generator;
  return google::protobuf::compiler::PluginMain(argc, argv, &generator);
}

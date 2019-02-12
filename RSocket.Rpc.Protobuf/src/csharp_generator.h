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

#ifndef RSOCKET_RPC_COMPILER_CSHARP_GENERATOR_H
#define RSOCKET_RPC_COMPILER_CSHARP_GENERATOR_H

#include <google/protobuf/descriptor.h>

using namespace std;

namespace rsocket_rpc_csharp_generator {

string GetServices(const google::protobuf::FileDescriptor* file,
                         bool generate_client, bool generate_server);

}  // namespace rsocket_rpc_csharp_generator

#endif  // RSOCKET_RPC_COMPILER_CSHARP_GENERATOR_H

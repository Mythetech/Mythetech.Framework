/**
 * Mythetech WebAssembly Shell Executor
 * Provides a command execution environment for Blazor WebAssembly applications.
 */

window.mythetech = window.mythetech || {};

window.mythetech.shell = {
    /**
     * Registered command handlers.
     * @type {Map<string, function>}
     */
    commands: new Map(),

    /**
     * Active processes.
     * @type {Map<number, object>}
     */
    processes: new Map(),

    /**
     * Next process ID to assign.
     * @type {number}
     */
    nextProcessId: 1,

    /**
     * Registers a command handler.
     * @param {string} name - The command name.
     * @param {function} handler - Async function that receives (args, env) and returns { exitCode, stdout, stderr }.
     */
    registerCommand(name, handler) {
        this.commands.set(name.toLowerCase(), handler);
    },

    /**
     * Unregisters a command.
     * @param {string} name - The command name to remove.
     * @returns {boolean} True if the command was found and removed.
     */
    unregisterCommand(name) {
        return this.commands.delete(name.toLowerCase());
    },

    /**
     * Gets all registered command names.
     * @returns {string[]} Array of command names.
     */
    getCommands() {
        return Array.from(this.commands.keys()).sort();
    },

    /**
     * Executes a command.
     * @param {string} command - The command name.
     * @param {string} args - The command arguments as a string.
     * @param {object} env - Environment variables.
     * @returns {Promise<{found: boolean, exitCode: number, standardOutput: string, standardError: string}>}
     */
    async execute(command, args, env) {
        const handler = this.commands.get(command.toLowerCase());

        if (!handler) {
            return {
                found: false,
                exitCode: 127,
                standardOutput: '',
                standardError: `Command not found: ${command}`
            };
        }

        try {
            const argArray = this._parseArgs(args || '');
            const result = await handler(argArray, env || {});

            return {
                found: true,
                exitCode: result.exitCode ?? 0,
                standardOutput: result.stdout ?? '',
                standardError: result.stderr ?? ''
            };
        } catch (e) {
            return {
                found: true,
                exitCode: 1,
                standardOutput: '',
                standardError: e.message || String(e)
            };
        }
    },

    /**
     * Creates an interactive process.
     * @param {string} command - The command name.
     * @param {object} dotNetRef - Reference to the .NET WasmShellProcess object.
     * @returns {number|null} Process handle, or null if no handler exists.
     */
    createProcess(command, dotNetRef) {
        const handler = this.commands.get(command.toLowerCase());

        if (!handler) {
            return null;
        }

        const id = this.nextProcessId++;
        const process = {
            id,
            command,
            dotNetRef,
            aborted: false,
            inputBuffer: [],
            inputCallback: null
        };

        this.processes.set(id, process);

        // Start the process asynchronously
        this._runProcess(process, handler);

        return id;
    },

    /**
     * Writes input to a process.
     * @param {number} processId - The process handle.
     * @param {string} data - The input data.
     */
    writeInput(processId, data) {
        const proc = this.processes.get(processId);
        if (!proc) return;

        if (proc.inputCallback) {
            proc.inputCallback(data);
        } else {
            proc.inputBuffer.push(data);
        }
    },

    /**
     * Interrupts a process (signals cancellation).
     * @param {number} processId - The process handle.
     */
    interrupt(processId) {
        const proc = this.processes.get(processId);
        if (proc) {
            proc.aborted = true;
            if (proc.abortController) {
                proc.abortController.abort();
            }
        }
    },

    /**
     * Forcefully terminates a process.
     * @param {number} processId - The process handle.
     */
    kill(processId) {
        const proc = this.processes.get(processId);
        if (proc) {
            proc.aborted = true;
            if (proc.abortController) {
                proc.abortController.abort();
            }
            this._notifyExit(proc, -1);
        }
    },

    /**
     * Disposes a process and cleans up resources.
     * @param {number} processId - The process handle.
     */
    dispose(processId) {
        this.processes.delete(processId);
    },

    /**
     * Internal: Runs a process.
     * @private
     */
    async _runProcess(process, handler) {
        const abortController = new AbortController();
        process.abortController = abortController;

        // Create a context for the process
        const context = {
            write: (data) => this._notifyOutput(process, data),
            writeError: (data) => this._notifyError(process, data),
            readLine: () => this._readLine(process),
            isAborted: () => process.aborted,
            signal: abortController.signal
        };

        try {
            const result = await handler([], {}, context);
            if (!process.aborted) {
                this._notifyExit(process, result?.exitCode ?? 0);
            }
        } catch (e) {
            if (!process.aborted) {
                this._notifyError(process, e.message || String(e));
                this._notifyExit(process, 1);
            }
        }
    },

    /**
     * Internal: Reads a line of input from the process.
     * @private
     */
    _readLine(process) {
        return new Promise((resolve) => {
            if (process.inputBuffer.length > 0) {
                resolve(process.inputBuffer.shift());
            } else {
                process.inputCallback = (data) => {
                    process.inputCallback = null;
                    resolve(data);
                };
            }
        });
    },

    /**
     * Internal: Notifies .NET of output.
     * @private
     */
    _notifyOutput(process, data) {
        if (process.dotNetRef && !process.aborted) {
            process.dotNetRef.invokeMethodAsync('OnOutput', data);
        }
    },

    /**
     * Internal: Notifies .NET of error output.
     * @private
     */
    _notifyError(process, data) {
        if (process.dotNetRef && !process.aborted) {
            process.dotNetRef.invokeMethodAsync('OnError', data);
        }
    },

    /**
     * Internal: Notifies .NET of process exit.
     * @private
     */
    _notifyExit(process, exitCode) {
        if (process.dotNetRef) {
            process.dotNetRef.invokeMethodAsync('OnExit', exitCode);
        }
        this.processes.delete(process.id);
    },

    /**
     * Internal: Parses an argument string into an array.
     * @private
     */
    _parseArgs(argsString) {
        if (!argsString || !argsString.trim()) {
            return [];
        }

        const args = [];
        let current = '';
        let inQuotes = false;
        let quoteChar = '';

        for (const c of argsString) {
            if (!inQuotes && (c === '"' || c === "'")) {
                inQuotes = true;
                quoteChar = c;
            } else if (inQuotes && c === quoteChar) {
                inQuotes = false;
                quoteChar = '';
            } else if (!inQuotes && /\s/.test(c)) {
                if (current) {
                    args.push(current);
                    current = '';
                }
            } else {
                current += c;
            }
        }

        if (current) {
            args.push(current);
        }

        return args;
    }
};

// Register built-in commands
(function() {
    const shell = window.mythetech.shell;

    // echo - prints arguments
    shell.registerCommand('echo', (args) => ({
        exitCode: 0,
        stdout: args.join(' ')
    }));

    // env - prints environment variables
    shell.registerCommand('env', (args, env) => ({
        exitCode: 0,
        stdout: Object.entries(env)
            .map(([k, v]) => `${k}=${v}`)
            .join('\n')
    }));

    // help - lists available commands
    shell.registerCommand('help', () => ({
        exitCode: 0,
        stdout: 'Available commands:\n' + shell.getCommands().join('\n')
    }));

    // clear - clears the console (no-op in terms of output)
    shell.registerCommand('clear', () => ({
        exitCode: 0,
        stdout: ''
    }));

    // version - prints shell version
    shell.registerCommand('version', () => ({
        exitCode: 0,
        stdout: 'Mythetech WebAssembly Shell v1.0.0'
    }));
})();

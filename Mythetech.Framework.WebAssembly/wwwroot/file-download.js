/**
 * Mythetech WebAssembly file download
 * Saves a file through a regular browser download, for browsers without the File System Access API.
 */

// Firefox and Safari can drop a download whose object URL is revoked before the download starts,
// and that start happens after click() returns, so the URL has to outlive the click.
const revokeDelayMs = 40000;

/**
 * Downloads the given bytes as a file.
 * @param {string} fileName - The suggested file name.
 * @param {string} mimeType - The MIME type of the file.
 * @param {Uint8Array} data - The file contents.
 */
export function downloadFile(fileName, mimeType, data) {
    const blob = new Blob([data], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.style.display = 'none';
    document.body.appendChild(anchor);

    try {
        anchor.click();
    } finally {
        anchor.remove();
        setTimeout(() => URL.revokeObjectURL(url), revokeDelayMs);
    }
}

export async function importFromSource(code) {
    const url = URL.createObjectURL(new Blob([code], { type: "text/javascript" }));
    try {
        return await import(url);
    } finally {
        URL.revokeObjectURL(url);
    }
}

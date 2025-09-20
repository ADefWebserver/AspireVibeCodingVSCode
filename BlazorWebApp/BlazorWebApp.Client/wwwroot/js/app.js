// File download utility for Blazor WebAssembly
window.downloadFile = function (fileName, contentType, data) {
    const blob = new Blob([new Uint8Array(data)], { type: contentType });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    
    URL.revokeObjectURL(url);
};

// Virtual File System Access API for saving files locally
window.saveFileToLocal = async function (fileName, contentType, data) {
    if ('showSaveFilePicker' in window) {
        try {
            const fileHandle = await window.showSaveFilePicker({
                suggestedName: fileName,
                types: [{
                    description: 'Files',
                    accept: {
                        [contentType]: [`.${fileName.split('.').pop()}`]
                    }
                }]
            });
            
            const writable = await fileHandle.createWritable();
            await writable.write(new Uint8Array(data));
            await writable.close();
            
            return true;
        } catch (err) {
            console.warn('File save cancelled or failed:', err);
            // Fallback to download
            window.downloadFile(fileName, contentType, data);
            return false;
        }
    } else {
        // Fallback to download for browsers that don't support File System Access API
        window.downloadFile(fileName, contentType, data);
        return false;
    }
};
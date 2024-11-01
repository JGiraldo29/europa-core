/*
 * Migairu Europa - End-to-End Encrypted File Transfer
 * Copyright (C) 2024 Migairu Corp.
 * Written by Juan Miguel Giraldo.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, see <https://www.gnu.org/licenses/>.
 */

document.addEventListener('DOMContentLoaded', function () {
    const downloadButton = document.getElementById('downloadButton');
    const passphraseForm = document.getElementById('passphraseForm');
    const passphraseInput = document.getElementById('passphraseInput');
    const submitPassphrase = document.getElementById('submitPassphrase');
    const fileId = downloadButton.dataset.fileid;
    let passphrase = decodeURIComponent(window.location.hash.slice(1));

    if (passphraseForm) {
        submitPassphrase.addEventListener('click', function () {
            passphrase = passphraseInput.value;
            if (passphrase) {
                passphraseForm.style.display = 'none';
                downloadButton.style.display = 'block';
            } else {
                showErrorNotification('Please enter a passphrase.');
            }
        });
    }

    downloadButton.addEventListener('click', async () => {
        showLoadingStage(1);

        if (!passphrase) {
            showErrorNotification('The passphrase is missing. Please add the passphrase to the URL after a # symbol. (e.g. www.europa.migairu.com/d/abc123#your-passkey).');
            hideLoadingIndicator();
            return;
        }

        try {
            const response = await fetch(`/download-file/${fileId}`);
            const downloadWarning = document.getElementById('downloadWarning');
            if (!response.ok) {
                console.error('Download failed. Please try again.');
                hideLoadingIndicator();
            }

            const encryptedData = await response.arrayBuffer();
            const ivBase64 = response.headers.get('X-IV');
            const saltBase64 = response.headers.get('X-Salt');
            const isMultiFile = response.headers.get('X-Is-Multi-File') === 'true';

            const iv = new Uint8Array(atob(ivBase64).split('').map(char => char.charCodeAt(0)));
            const salt = new Uint8Array(atob(saltBase64).split('').map(char => char.charCodeAt(0)));

            const { metadata, fileContent } = await decryptFileAndMetadata(encryptedData, iv, salt, passphrase);

            if (isMultiFile) {
                const zip = await JSZip.loadAsync(fileContent);
                const zipBlob = await zip.generateAsync({ type: "blob" });
                const url = URL.createObjectURL(zipBlob);
                const a = document.createElement('a');
                a.href = url;
                a.download = "downloaded_files.zip";
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
                downloadWarning.textContent = 'Your file is ready! Please wait for your download to start.';
                hideLoadingIndicator();
            } else {
                const blob = new Blob([fileContent], { type: metadata.fileType });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = metadata.fileName;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
                downloadWarning.textContent = 'Your file is ready! Please wait for your download to start.';
                hideLoadingIndicator();
            }
        } catch (error) {
            console.error('Decryption failed. The file may be corrupted or the passphrase is incorrect.');
            hideLoadingIndicator();
        }
    });
});
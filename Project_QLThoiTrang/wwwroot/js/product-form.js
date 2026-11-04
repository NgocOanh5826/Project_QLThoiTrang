document.querySelectorAll('[data-product-form]').forEach((form) => {
    const file = form.querySelector('[data-product-image-file]');
    const library = form.querySelector('[name="Image"]');
    const preview = form.querySelector('[data-product-image-preview]');
    let temporaryUrl;

    // updatePreview: Kiểm tra ảnh được chọn và cập nhật phần xem trước.
    function updatePreview() {
        if (temporaryUrl) URL.revokeObjectURL(temporaryUrl);
        temporaryUrl = undefined;
        file.setCustomValidity('');
        const image = file.files[0];
        if (image && (image.size > 5 * 1024 * 1024 || !/^image\/(jpeg|png|gif|webp)$/.test(image.type))) {
            file.setCustomValidity('Chọn ảnh JPG, PNG, GIF hoặc WebP, tối đa 5 MB.');
            file.reportValidity();
            preview.src = library.value;
            return;
        }
        if (image) temporaryUrl = URL.createObjectURL(image);
        preview.src = temporaryUrl || library.value;
    }
    file.addEventListener('change', updatePreview);
    library.addEventListener('change', () => {
        file.value = '';
        updatePreview();
    });
    window.addEventListener('pagehide', () => {
        if (temporaryUrl) URL.revokeObjectURL(temporaryUrl);
    });
});

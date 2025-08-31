// Admin JavaScript

// Initialize tooltips
var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});

// Initialize popovers
var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
    return new bootstrap.Popover(popoverTriggerEl);
});

// Toggle sidebar on mobile
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (window.innerWidth <= 768) {
        sidebar.classList.toggle('d-none');
    }
}

// Close sidebar when clicking outside on mobile
document.addEventListener('click', function(event) {
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    
    if (window.innerWidth <= 768 && 
        !sidebar.contains(event.target) && 
        !sidebarToggle.contains(event.target) &&
        !sidebar.classList.contains('d-none')) {
        sidebar.classList.add('d-none');
    }
});

// Handle image preview for file inputs
function handleImagePreview(input, previewId) {
    const preview = document.getElementById(previewId);
    const file = input.files[0];
    const reader = new FileReader();

    reader.onloadend = function () {
        preview.src = reader.result;
        preview.style.display = 'block';
    }

    if (file) {
        reader.readAsDataURL(file);
    } else {
        preview.src = "";
        preview.style.display = 'none';
    }
}

// Initialize tag inputs
function initTagInput(inputId, tagContainerId) {
    const tagInput = document.getElementById(inputId);
    const tagContainer = document.getElementById(tagContainerId);
    
    if (!tagInput || !tagContainer) return;
    
    // Load existing tags
    updateTagDisplay();
    
    // Handle tag input
    tagInput.addEventListener('keydown', function(e) {
        if (e.key === 'Enter' || e.key === ',') {
            e.preventDefault();
            addTag(this.value.trim());
            this.value = '';
        }
    });
    
    // Handle paste
    tagInput.addEventListener('paste', function(e) {
        e.preventDefault();
        const text = (e.clipboardData || window.clipboardData).getData('text');
        const tags = text.split(',').map(tag => tag.trim()).filter(tag => tag.length > 0);
        
        tags.forEach(tag => {
            addTag(tag);
        });
    });
    
    // Add tag function
    function addTag(tagText) {
        if (!tagText) return;
        
        // Add to hidden input
        const hiddenInput = document.createElement('input');
        hiddenInput.type = 'hidden';
        hiddenInput.name = 'Tags';
        hiddenInput.value = tagText;
        tagContainer.appendChild(hiddenInput);
        
        // Add to display
        updateTagDisplay();
    }
    
    // Update tag display
    function updateTagDisplay() {
        const tags = Array.from(tagContainer.querySelectorAll('input[type="hidden"]')).map(input => input.value);
        const tagDisplay = tagContainer.querySelector('.tag-display') || document.createElement('div');
        tagDisplay.className = 'tag-display d-flex flex-wrap gap-2 my-2';
        
        tagDisplay.innerHTML = tags.map(tag => `
            <span class="badge bg-primary d-flex align-items-center">
                ${tag}
                <button type="button" class="btn-close btn-close-white btn-sm ms-2" aria-label="Remove" data-tag="${tag}"></button>
            </span>
        `).join('');
        
        // Add event listeners to remove buttons
        tagDisplay.querySelectorAll('button').forEach(button => {
            button.addEventListener('click', function() {
                const tagToRemove = this.getAttribute('data-tag');
                const inputs = tagContainer.querySelectorAll('input[type="hidden"]');
                inputs.forEach(input => {
                    if (input.value === tagToRemove) {
                        input.remove();
                    }
                });
                updateTagDisplay();
            });
        });
        
        if (!tagContainer.contains(tagDisplay)) {
            tagContainer.appendChild(tagDisplay);
        }
    }
}

// Initialize WYSIWYG editor
document.addEventListener('DOMContentLoaded', function() {
    // Initialize any WYSIWYG editors
    if (typeof tinymce !== 'undefined') {
        tinymce.init({
            selector: '.wysiwyg-editor',
            plugins: 'link image code table lists media',
            toolbar: 'undo redo | formatselect | bold italic | alignleft aligncenter alignright | bullist numlist | link image media | code',
            height: 400,
            menubar: false,
            statusbar: false,
            content_style: 'body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif; font-size: 14px; }',
            images_upload_url: '/admin/upload',
            images_upload_credentials: true,
            automatic_uploads: true
        });
    }
    
    // Initialize tag inputs
    initTagInput('tag-input', 'tag-container');
    
    // Handle form submissions with confirmation
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!confirm(this.getAttribute('data-confirm'))) {
                e.preventDefault();
            }
        });
    });
    
    // Handle image previews
    document.querySelectorAll('.image-upload').forEach(input => {
        input.addEventListener('change', function() {
            const previewId = this.getAttribute('data-preview');
            if (previewId) {
                handleImagePreview(this, previewId);
            }
        });
    });
    
    // Toggle password visibility
    document.querySelectorAll('.toggle-password').forEach(button => {
        button.addEventListener('click', function() {
            const input = document.querySelector(this.getAttribute('data-target'));
            if (input) {
                const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
                input.setAttribute('type', type);
                this.querySelector('i').classList.toggle('fa-eye');
                this.querySelector('i').classList.toggle('fa-eye-slash');
            }
        });
    });
    
    // Handle bulk actions
    const bulkAction = document.getElementById('bulk-action');
    const bulkForm = document.getElementById('bulk-form');
    
    if (bulkAction && bulkForm) {
        bulkAction.addEventListener('change', function() {
            if (this.value) {
                if (confirm(`Are you sure you want to ${this.options[this.selectedIndex].text.toLowerCase()} the selected items?`)) {
                    bulkForm.submit();
                } else {
                    this.selectedIndex = 0;
                }
            }
        });
    }
    
    // Toggle all checkboxes
    const toggleAll = document.getElementById('toggle-all');
    if (toggleAll) {
        toggleAll.addEventListener('change', function() {
            const checkboxes = document.querySelectorAll('.item-checkbox');
            checkboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
            });
        });
    }
    
    // Update bulk action button state
    const itemCheckboxes = document.querySelectorAll('.item-checkbox');
    if (itemCheckboxes.length > 0) {
        const bulkActions = document.querySelectorAll('.bulk-actions select, .bulk-actions button');
        
        const updateBulkActions = () => {
            const checkedCount = document.querySelectorAll('.item-checkbox:checked').length;
            const isAnyChecked = checkedCount > 0;
            
            bulkActions.forEach(action => {
                action.disabled = !isAnyChecked;
                
                if (action.tagName === 'BUTTON') {
                    const countSpan = action.querySelector('.selected-count');
                    if (countSpan) {
                        countSpan.textContent = checkedCount;
                    }
                }
            });
        };
        
        itemCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', updateBulkActions);
        });
        
        // Initial update
        updateBulkActions();
    }
});

// Handle AJAX form submissions
function submitFormAjax(form, onSuccess, onError) {
    const formData = new FormData(form);
    const url = form.getAttribute('action');
    const method = form.getAttribute('method') || 'POST';
    
    fetch(url, {
        method: method,
        body: formData,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (typeof onSuccess === 'function') {
                onSuccess(data);
            }
        } else {
            if (typeof onError === 'function') {
                onError(data.errors || 'An error occurred');
            }
        }
    })
    .catch(error => {
        console.error('Error:', error);
        if (typeof onError === 'function') {
            onError('An error occurred while processing your request');
        }
    });
}

// Show toast notifications
function showToast(type, message) {
    const toastContainer = document.getElementById('toast-container') || createToastContainer();
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    
    toast.id = toastId;
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    
    const bsToast = new bootstrap.Toast(toast, {
        autohide: true,
        delay: 5000
    });
    
    bsToast.show();
    
    // Remove toast after it's hidden
    toast.addEventListener('hidden.bs.toast', function() {
        toast.remove();
    });
}

// Create toast container if it doesn't exist
function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    container.style.zIndex = '1100';
    document.body.appendChild(container);
    return container;
}

// Handle AJAX file uploads
function uploadFile(file, url, onProgress, onSuccess, onError) {
    const formData = new FormData();
    formData.append('file', file);
    
    const xhr = new XMLHttpRequest();
    
    xhr.upload.addEventListener('progress', function(e) {
        if (e.lengthComputable && typeof onProgress === 'function') {
            const percentComplete = (e.loaded / e.total) * 100;
            onProgress(percentComplete);
        }
    });
    
    xhr.addEventListener('load', function() {
        if (xhr.status >= 200 && xhr.status < 300) {
            try {
                const response = JSON.parse(xhr.responseText);
                if (typeof onSuccess === 'function') {
                    onSuccess(response);
                }
            } catch (e) {
                if (typeof onError === 'function') {
                    onError('Error parsing server response');
                }
            }
        } else {
            if (typeof onError === 'function') {
                onError(xhr.statusText || 'Upload failed');
            }
        }
    });
    
    xhr.addEventListener('error', function() {
        if (typeof onError === 'function') {
            onError('Network error occurred');
        }
    });
    
    xhr.open('POST', url, true);
    xhr.send(formData);
}

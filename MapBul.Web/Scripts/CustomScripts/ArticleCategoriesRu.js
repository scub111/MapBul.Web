﻿function OnArticleCategoriesDocumentReady() {
    $('#nestableArticle').nestable({}).on('change', OnArticleCategoriesStructureChanged);
    $("#NewArticleCategoryFormSubmit").click(SendNewArticleCategoryForm);
    $("#EditArticleCategoryButton").click(OnEditArticleCategoryClick);
    jQuery.extend(jQuery.validator.messages, {
        required: "Заполните поле"
    });
    $('.colorInput').colorpicker();
}

function OnEditArticleCategoryDocumentReady() {
    $('.colorInputCategoryEdit').colorpicker();
    $("#EditCategoryFormSubmit").click(SendEditArticleCategoryForm);
    $("#DeleteCategoryButton").click(OnDeleteArticleCategoryClick);
}

function SendEditArticleCategoryForm() {
    var form = document.getElementById("EditCategoryForm");
    var formData = new FormData(form);
    var file = document.getElementById("EditCategoryIconInput").files[0];
    formData.append("CategoryIcon", file);

    var file = document.getElementById("EditCategoryPinInput").files[0];
    formData.append("CategoryPin", file);

    $.ajax({
        url: "Dictionaries/EditCategory",
        data: formData,
        contentType: false,
        processData: false,
        type: "POST",
        success: function() {
            AddNewArticleCategorySuccess();
            $("#Modal").modal("hide");
        },
        error:function() {
            ViewNotification("Ошибка","error");
        }
    });
    return false;
}

function OnEditArticleCategoryClick() {
    var categoryId = $("#EditArticleCategorySelect").val();
    $.ajax({
        url: "Dictionaries/_EditCategoryModalPartial",
        type: "POST",
        data:{categoryId:categoryId},
        success: function (data) {
            $("#ModalContent").html(data);
            $("#Modal").modal("show");
            OnEditArticleCategoryDocumentReady();
        },
        error: function () {
            ViewNotification('Ошибка', 'error');
        }
    });
}

function OnDeleteArticleCategoryClick() {
    var categoryId = $("#EditCategoryForm input[name=Id]").val();
    $.ajax({
        url: "Dictionaries/DeleteCategory",
        type: "POST",
        data: { categoryId: categoryId },
        success: function (data) {
            if (data.success) {
                $("#Modal").modal("hide");
                RefreshArticleCategoriesPage();
            }
        },
        error: function () {
            ViewNotification('Ошибка', 'error');
        }
    });
}


function SendNewArticleCategoryForm() {
    if (!($("#NewArticleCategoryForm").valid())) {
        ViewNotification("Заполните обязательные поля!", "error");
        return 0;
    }
    var form = document.getElementById("NewArticleCategoryForm");
    var formData = new FormData(form);
    var file = document.getElementById("NewArticleCategoryIconInput").files[0];
    formData.append("ArticleCategoryIcon", file);
    file = document.getElementById("NewArticleCategoryPinInput").files[0];
    formData.append("ArticleCategoryPin", file);
    $.ajax({
        url: "Dictionaries/AddNewCategory",
        data: formData,
        contentType: false,
        processData: false,
        type: "POST",
        success: AddNewArticleCategorySuccess,
        error: function() {
            ViewNotification("Ошибка", "error");
        }
    });
    return false;
}

function RefreshArticleCategoriesPage() {
    $.ajax({
        url: "Dictionaries/_ArticleCategoriesPartial",
        type: "POST",
        success: function (data) {
            $("#ArticleCategoriesContainer").html(data);
            OnArticleCategoriesDocumentReady();
        },
        error: function () {
            ViewNotification('Ошибка', 'error');
        }
    });
}

function AddNewArticleCategorySuccess() {
    ViewNotification("Категория сохранена", "success");
    RefreshArticleCategoriesPage();
}

function OnArticleCategoriesStructureChanged(e) {
    var list = e.length ? e : $(e.target);
    var string = window.JSON.stringify(list.nestable('serialize'));
    SendNewStructure(string);
}

function SendNewStructure(string) {
    var url = "Dictionaries/SaveCategoriesStructure";
    $.ajax({
        url: url,
        type: "POST",
        data: { structure: string },
        success: function () {
            ViewNotification("Изменения сохранены", 'success');
        },
        error: function () {
            ViewNotification("Не удалось сохранить", 'error');
        }
    });
}

$(document).ready(OnArticleCategoriesDocumentReady);